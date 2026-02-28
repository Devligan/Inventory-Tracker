public class NonPerishableItem : Item
{
	public NonPerishableItem(string name, double quantity) : base(name, quantity) { }

	public override string GetDetails() =>
		$"Non-Perishable | {Name} | Qty: {Quantity}";
}