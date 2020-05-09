using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

		public static InventoryPermission WithdrawalForJobs(params Jobs[] jobs) => SpecifiedForJobs(Types.Withdrawal, jobs);

		public static InventoryPermission DepositForJobs(params Jobs[] jobs) => SpecifiedForJobs(Types.Deposit, jobs);

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

		public Types GetPermission(Jobs job)
		{
			return Entries.TryGetValue(job, out var result) ? result : Default;
		}

		public bool HasPermission(Jobs job, Types type) => (GetPermission(job) & type) == type;

		public bool CanWithdrawal(Jobs job) => HasPermission(job, Types.Withdrawal);
		public bool CanDeposit(Jobs job) => HasPermission(job, Types.Deposit);
	}
}