using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Ultrabolt.SkyEngine
{
	public class WorldTimeEvent : MonoBehaviour
	{
		public List<GameEvent> gameEvents;

		public void TriggerEventsFor(int day, GameTime time)
		{
			foreach (var e in gameEvents)
			{
				if ((e.recurring || e.day == day) && e.time == time && (e.recurring || !e.activated))
				{
					e.actions.Invoke();
					if (!e.recurring) e.activated = true;
				}
			}
		}

		[System.Serializable]
		public class GameEvent
		{
			public string eventName = "New Event";

			public int day = 1;
			public bool recurring;
			public GameTime time;

			public UnityEvent actions;

			[HideInInspector] public bool activated;

			public void AutoName() => eventName = $"Day {day} - At the {time}";
		}
	}
}