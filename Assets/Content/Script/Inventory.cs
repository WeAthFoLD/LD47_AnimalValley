using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using SPlay;
using UnityEngine;

public enum StackType {
	Single,
	Multiple
}

[Serializable]
public class Item {
	public string id;
	public Sprite sprite;
	public StackType stackType = StackType.Multiple;

	public string name;
	[TextArea]
	public string desc;
	[TextArea]
	public string onUse;
	[EventRef]
	public string clickSound;

	public Expression onUseExpr;
	// public int maxNumber = -1;
	//
	// public int actualMaxNumber =>
	// 	maxNumber == -1 && stackType == StackType.Multiple ? ItemStack.MaxStackNum : maxNumber;
}

public class ItemStack {
	public const int MaxStackNum = 100;

	public Item item;
	public int number;

	public ItemStack()
	{ }

	public ItemStack(Item item, int number)
	{
		XDebug.Assert(item != null);
		XDebug.Assert(number > 0);
		this.item = item;
		this.number = number;
	}

	public ItemStack Copy() {
		return new ItemStack {
			item = item,
			number = number
		};
	}
}

public class Inventory {
	public readonly List<ItemStack> items = new List<ItemStack>();

	public void Add(ItemStack stack) {
		if (CountItem(stack.item) == 0) {
			items.Add(stack);
		} else {
			items.First(x => x.item == stack.item).number += stack.number;
		}
	}

	public int CountItem(Item it) {
		return items.FirstOrDefault(x => x.item == it)?.number ?? 0;
	}

	public bool Consume(Item item, int count) {
		for (int i = 0; i < items.Count; ++i) {
			if (items[i].item == item) {
				if (items[i].number >= count) {
					items[i].number -= count;
					if (items[i].number == 0) {
						items[i] = null;
					}
					return true;
				} else {
					return false;
				}
			}
		}

		return false;
	}
}

