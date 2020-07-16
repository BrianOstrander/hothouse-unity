using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct InventoryPermission
	{
		public static InventoryPermission NoneForAnyJob() => new InventoryPermission(Types.None);
		public static InventoryPermission AllForAnyJob() => new InventoryPermission(Types.Withdrawal | Types.Deposit);
		
		public static InventoryPermission SpecifiedForJobs(
			Types type,
			params Jobs[] jobs
		)
		{
			return new InventoryPermission(
				Types.None,
				new ReadOnlyDictionary<Jobs, Types>(jobs.ToDictionary(job => job, job => type))
			);
		}
		
		public static InventoryPermission SpecifiedForJobs(
			params (Jobs Job, Types type)[] entries
		)
		{
			return new InventoryPermission(
				Types.None,
				new ReadOnlyDictionary<Jobs, Types>(
					entries.ToDictionary(
						e => e.Job,
						e => e.type
					)
				)
			);
		}

		public static InventoryPermission WithdrawalForJobs(params Jobs[] jobs) => SpecifiedForJobs(Types.Withdrawal, jobs);

		public static InventoryPermission DepositForJobs(params Jobs[] jobs) => SpecifiedForJobs(Types.Deposit, jobs);
		
		public static InventoryPermission AllForJobs(params Jobs[] jobs) => SpecifiedForJobs(Types.Withdrawal | Types.Deposit, jobs);

		[Flags]
		public enum Types
		{
			None = 0,
			Withdrawal = 1,
			Deposit = 2
		}

		public readonly Types Default;
		public readonly ReadOnlyDictionary<Jobs, Types> Entries;

		InventoryPermission(
			Types @default,
			ReadOnlyDictionary<Jobs, Types> entries = null
		)
		{
			Default = @default;
			Entries = entries ?? new ReadOnlyDictionary<Jobs, Types>(new Dictionary<Jobs, Types>());
		}

		public Types GetPermission(AgentModel model)
		{
			switch (model)
			{
				case DwellerModel modelDweller:
					return Entries.TryGetValue(modelDweller.Job.Value, out var result) ? result : Default;
				default:
					Debug.LogError("Unrecognized agent type: " + model.GetType());
					return Default;
			}
		}

		public bool HasPermission(AgentModel model, Types type) => (GetPermission(model) & type) == type;

		public bool CanWithdrawal(AgentModel model) => HasPermission(model, Types.Withdrawal);
		public bool CanDeposit(AgentModel model) => HasPermission(model, Types.Deposit);
	}
}