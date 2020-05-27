using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IObligationModel : IEnterableModel
	{
		ListenerProperty<Obligation[]> Obligations { get; }
	}

	public static class IObligationModelExtensions
	{
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements
		)
		{
			return elements.SelectMany(e => e.Obligations.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> predicate
		)
		{
			return elements.Where(predicate).SelectMany(e => e.Obligations.Value.Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<Obligation, bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.Value.Where(predicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<IObligationModel, bool> modelPredicate,
			Func<Obligation, bool> obligationPredicate
		)
		{
			return elements.Where(modelPredicate).SelectMany(e => e.Obligations.Value.Where(obligationPredicate).Select(o => (e, o)));
		}
		
		public static IEnumerable<(IObligationModel Model, Obligation Obligation)> GetIndividualObligations(
			this IEnumerable<IObligationModel> elements,
			Func<(IObligationModel Model, Obligation Obligation), bool> predicate
		)
		{
			return elements.SelectMany(e => e.Obligations.Value.Select(o => (e, o))).Where(predicate);
		}
	}
}