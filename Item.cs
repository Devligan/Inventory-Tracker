using System;
public abstract class Item
{
    public string Name { get; }
    public double Quantity { get; private set; }

    protected Item(string name, double quantity)
    {
        Name = name;
        Quantity = quantity;
    }

    public void TakeItem(double quantity) => Quantity -= quantity;
    public void AddItem(double quantity) => Quantity += quantity;

    public abstract string GetDetails();
}