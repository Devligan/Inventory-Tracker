using System;
public class PerishableItem : Item
{
    public DateOnly ExpirationDate { get; }

    public PerishableItem(string name, double quantity, DateOnly expirationDate)
        : base(name, quantity)
    {
        ExpirationDate = expirationDate;
    }

    public override string GetDetails() =>
        $"Perishable | {Name} | Qty: {Quantity} | Expires: {ExpirationDate}";
}