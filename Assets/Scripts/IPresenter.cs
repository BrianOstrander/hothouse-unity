using System;

namespace LunraGames.SubLight 
{
	public interface IPresenter
	{
		Type ViewInterface { get; }
		bool UnBinded { get; }
	}
}