using DocCapture.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocCapture.Data
{
    public static class DbSeeder
    {
        public const string EncoderRole = "Encoder";
        public const string SupervisorRole = "Supervisor";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            var db = sp.GetRequiredService<ApplicationDbContext>();
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();

            await db.Database.MigrateAsync();

            foreach (var role in new[] { EncoderRole, SupervisorRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            await EnsureUserAsync(userManager, "encoder@demo.local", "Demo!2345", EncoderRole);
            await EnsureUserAsync(userManager, "supervisor@demo.local", "Demo!2345", SupervisorRole);

            if (!await db.FormTemplates.AnyAsync())
                await SeedTemplateAsync(db);
        }

        private static async Task EnsureUserAsync(
            UserManager<IdentityUser> userManager, string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Seed user {email} failed: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }

            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }

        private static async Task SeedTemplateAsync(ApplicationDbContext db)
        {
            var template = new FormTemplate
            {
                Name = "Sales Invoice",
                Description = "Standard supplier sales invoice",
                Fields = new List<FieldDefinition>
                {
                    new() { FieldKey = "invoice_no",     DisplayLabel = "Invoice No.",     DataType = FieldDataType.Text,     IsRequired = true,  DisplayOrder = 1, ValidationRegex = @"^[A-Za-z0-9\-]{3,20}$" },
                    new() { FieldKey = "invoice_date",   DisplayLabel = "Invoice Date",    DataType = FieldDataType.Date,     IsRequired = true,  DisplayOrder = 2 },
                    new() { FieldKey = "supplier_name",  DisplayLabel = "Supplier Name",   DataType = FieldDataType.Text,     IsRequired = true,  DisplayOrder = 3 },
                    new() { FieldKey = "supplier_tin",   DisplayLabel = "Supplier TIN",    DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 4, ValidationRegex = @"^\d{3}-\d{3}-\d{3}(-\d{3,5})?$" },
                    new() { FieldKey = "supplier_addr",  DisplayLabel = "Supplier Address",DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 5 },
                    new() { FieldKey = "customer_name",  DisplayLabel = "Customer Name",   DataType = FieldDataType.Text,     IsRequired = true,  DisplayOrder = 6 },
                    new() { FieldKey = "customer_tin",   DisplayLabel = "Customer TIN",    DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 7, ValidationRegex = @"^\d{3}-\d{3}-\d{3}(-\d{3,5})?$" },
                    new() { FieldKey = "po_number",      DisplayLabel = "PO Number",       DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 8 },
                    new() { FieldKey = "terms",          DisplayLabel = "Payment Terms",   DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 9 },
                    new() { FieldKey = "due_date",       DisplayLabel = "Due Date",        DataType = FieldDataType.Date,     IsRequired = false, DisplayOrder = 10 },
                    new() { FieldKey = "subtotal",       DisplayLabel = "Subtotal",        DataType = FieldDataType.Currency, IsRequired = true,  DisplayOrder = 11 },
                    new() { FieldKey = "vat_amount",     DisplayLabel = "VAT (12%)",       DataType = FieldDataType.Currency, IsRequired = false, DisplayOrder = 12 },
                    new() { FieldKey = "total_amount",   DisplayLabel = "Total Amount",    DataType = FieldDataType.Currency, IsRequired = true,  DisplayOrder = 13 },
                    new() { FieldKey = "qty_items",      DisplayLabel = "Item Count",      DataType = FieldDataType.Number,   IsRequired = false, DisplayOrder = 14 },
                    new() { FieldKey = "remarks",        DisplayLabel = "Remarks",         DataType = FieldDataType.Text,     IsRequired = false, DisplayOrder = 15 }
                }
            };

            db.FormTemplates.Add(template);
            await db.SaveChangesAsync();
        }
    }
}