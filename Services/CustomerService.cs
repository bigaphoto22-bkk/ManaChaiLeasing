using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public class CustomerService
{
    public List<Customer> SearchCustomers(string? keyword)
    {
        using AppDbContext db = new();

        IQueryable<Customer> query = db.Customers
            .AsNoTracking();

        string term = keyword?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(term))
        {
            string pattern = $"%{term}%";

            query = query.Where(customer =>
                EF.Functions.Like(customer.FirstName, pattern) ||
                EF.Functions.Like(customer.LastName, pattern) ||
                EF.Functions.Like(customer.FirstName + " " + customer.LastName, pattern) ||
                (customer.CitizenId != null &&
                    EF.Functions.Like(customer.CitizenId, pattern)) ||
                (customer.Phone != null &&
                    EF.Functions.Like(customer.Phone, pattern)) ||
                (customer.Address != null &&
                    EF.Functions.Like(customer.Address, pattern)));
        }

        return query
            .OrderByDescending(customer => customer.UpdatedAt)
            .ThenByDescending(customer => customer.Id)
            .Take(200)
            .ToList();
    }

    public Customer SaveCustomer(
        Customer input,
        int? selectedCustomerId)
    {
        using AppDbContext db = new();

        Customer? customer = null;

        if (selectedCustomerId.HasValue)
        {
            customer = db.Customers
                .FirstOrDefault(item => item.Id == selectedCustomerId.Value);

            if (customer is null)
            {
                throw new InvalidOperationException(
                    "ไม่พบข้อมูลลูกค้าที่เลือกไว้ กรุณาค้นหาลูกค้าใหม่อีกครั้ง");
            }
        }
        else if (!string.IsNullOrWhiteSpace(input.CitizenId))
        {
            customer = db.Customers
                .FirstOrDefault(item => item.CitizenId == input.CitizenId);
        }

        if (!string.IsNullOrWhiteSpace(input.CitizenId))
        {
            Customer? duplicateCitizenIdCustomer = db.Customers
                .AsNoTracking()
                .FirstOrDefault(item =>
                    item.CitizenId == input.CitizenId &&
                    (!selectedCustomerId.HasValue ||
                     item.Id != selectedCustomerId.Value));

            if (duplicateCitizenIdCustomer is not null &&
                customer is null)
            {
                throw new InvalidOperationException(
                    "เลขบัตรประชาชนนี้มีข้อมูลลูกค้าอยู่แล้ว กรุณาใช้ปุ่มค้นหาลูกค้าเก่า");
            }

            if (duplicateCitizenIdCustomer is not null &&
                selectedCustomerId.HasValue &&
                customer is not null &&
                customer.CitizenId != input.CitizenId)
            {
                throw new InvalidOperationException(
                    "เลขบัตรประชาชนนี้ถูกใช้กับลูกค้าคนอื่นแล้ว");
            }
        }

        if (customer is null)
        {
            customer = new Customer
            {
                CreatedAt = DateTime.Now
            };

            db.Customers.Add(customer);
        }

        customer.FirstName = input.FirstName.Trim();
        customer.LastName = input.LastName.Trim();
        customer.CitizenId = CleanOptional(input.CitizenId);
        customer.Age = input.Age;
        customer.Phone = CleanOptional(input.Phone);
        customer.Address = CleanOptional(input.Address);
        customer.UpdatedAt = DateTime.Now;

        db.SaveChanges();

        return db.Customers
            .AsNoTracking()
            .Single(item => item.Id == customer.Id);
    }

    private static string? CleanOptional(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(cleaned)
            ? null
            : cleaned;
    }
}
