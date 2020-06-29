using System;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class BaseInventoryTransferState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel 
	{
		public override string Name => "InventoryTransfer";
		
		public override void OnInitialize()
		{
			AddTransitions(
				
			);
		}

		public override void Begin()
		{
			
		}

		public override void Idle()
		{
			
		}

		public override void End()
		{
			
		}
	}
}