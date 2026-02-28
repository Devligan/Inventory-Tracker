# Inventory Tracker

## Overview
This is a simple inventory tracking application written in C#. It allows users to manage items in an inventory, including perishable and non-perishable products. Users can add, remove, and list items, and the application maintains a log of actions.

## Features
- Add new inventory items
- Remove existing items
- Categorize items as perishable or non-perishable
- View all items in the inventory
- Simple logging of actions and changes
- Data persistence using a text file (`data.txt`)

## Project Structure
- `Item.cs` - Defines the properties and behavior of an inventory item.
- `NonPerishableItem.cs` / `PerishibleItem.cs` - Classes representing specific types of items.
- `Inventory.cs` - Handles the main inventory collection and operations.
- `ItemComparer.cs` - Provides comparison logic for sorting or searching items.
- `MainForm.cs` - Main UI for interacting with the inventory.
- `Main.cs` - Application entry point.
- `data.txt` - Stores inventory data.
- `log.txt` - Records actions and changes for auditing purposes.
