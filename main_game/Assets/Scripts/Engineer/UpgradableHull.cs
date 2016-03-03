﻿using System;

/// <summary>
/// An upgradable hull.
/// </summary>
public class UpgradableHull : UpgradableComponent
{
	/// <summary>
	/// The maximum ship health.
	/// </summary>
	/// <value>The maximum health value.</value>
	public int MaxHealth { get; private set; }

	public UpgradableHull() : base()
	{
		this.Type = ComponentType.Bridge;
		this.MaxHealth = this.Health = 100; // TODO: read this from GameSettings
	}

	// TODO: balance values

	/// <summary>
	/// Gets the efficiency of the component. Efficiency does not decreases when the hull is damaged.
	/// </summary>
	/// <returns>1.</returns>
	public override float GetEfficiency()
	{
		return 1; // The hull doesn't lose efficiency with damage
	}
		
	/// <summary>
	/// Upgrades this component to the next level. Even levels give a boost to forward speed,
	/// odd levels give a boost to turning speed.
	/// </summary>
	public override void Upgrade()
	{
		base.Upgrade();

		MaxHealth *= 2;
	}
}
