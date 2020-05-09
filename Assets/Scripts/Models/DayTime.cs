using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
#pragma warning disable CS0661 // Defines == or != operator but does not override Ojbect.GetHashCode()
#pragma warning disable CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
	public struct DayTime : IEquatable<DayTime>
#pragma warning restore CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Defines == or != operator but does not override Ojbect.GetHashCode()
	{
		public const float DaysInYear = 365f;
		public const float MonthsInYear = 12f;
		public const float DaysInMonth = 30f;
		public const float TimeInDay = 1f;

		public static DayTime Zero => new DayTime();
		public static DayTime MaxValue => new DayTime(int.MaxValue);

		public static DayTime FromYear(float years)
		{
			var wholeYears = Mathf.FloorToInt(years);
			return new DayTime(wholeYears * Mathf.FloorToInt(DaysInYear), (years - wholeYears) * DaysInYear);
		}

		/// <summary>
		/// Gets the current DayTime with a Time of zero.
		/// </summary>
		/// <value>The time zero.</value>
		[JsonIgnore]
		public DayTime TimeZero => new DayTime(Day, 0f);
		/// <summary>
		/// Gets the current DayTime with a Day of zero.
		/// </summary>
		/// <value>The time zero.</value>
		[JsonIgnore]
		public DayTime DayZero => new DayTime(0, Time);

		/// <summary>
		/// The Day component of this DayTime.
		/// </summary>
		[JsonProperty] public readonly int Day;
		/// <summary>
		/// The Time component of this DayTime.
		/// </summary>
		[JsonProperty] public readonly float Time;

		/// <summary>
		/// The Year component of this DayTime.
		/// </summary>
		/// <value>The years.</value>
		[JsonIgnore]
		public int Year => Mathf.FloorToInt(TotalYears);

		/// <summary>
		/// Gets the total time.
		/// </summary>
		/// <value>The total time.</value>
		[JsonIgnore]
		public float TotalTime => Day + Time;

		/// <summary>
		/// Gets the years.
		/// </summary>
		/// <value>The years.</value>
		[JsonIgnore]
		public float TotalYears => TotalTime / DaysInYear;

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:LunraGames.SpaceFarm.DayTime"/> is zero.
		/// </summary>
		/// <value><c>true</c> if is zero; otherwise, <c>false</c>.</value>
		[JsonIgnore]
		public bool IsZero => Day == 0 && Mathf.Approximately(0f, Time);

		public DayTime(DayTime other) : this(other.Day, other.Time) {}

		public DayTime(float time)
		{
			var newTime = time % TimeInDay;
			var dayTime = time - newTime;
			Day = Mathf.FloorToInt(dayTime / TimeInDay);
			Time = newTime;
		}

		public DayTime(int day)
		{
			Day = day;
			Time = 0f;
		}

		public DayTime(int day, float time) : this(time)
		{
			Day += day;
		}

		public void GetValues(out int years, out int months, out int days)
		{
			var dayRemainder = Day % DaysInYear;
			years = Year;
			var monthRemainder = dayRemainder % DaysInMonth;
			months = (int)((dayRemainder - monthRemainder) / DaysInMonth);
			days = Mathf.FloorToInt(monthRemainder);
		}

		/// <summary>
		/// Add the specified day and time and returns a new DayTime object containing the result.
		/// </summary>
		/// <returns>The result of the addition.</returns>
		/// <param name="day">Day.</param>
		/// <param name="time">Time.</param>
		public DayTime Add(int day, float time)
		{
			var dayResult = Day;
			var timeResult = Time;
			time += timeResult;
			var newTime = time % TimeInDay;
			var dayTime = time - newTime;
			dayResult += day + Mathf.FloorToInt(dayTime / TimeInDay);
			timeResult = newTime;
			return new DayTime(dayResult, timeResult);
		}

		/// <summary>
		/// Multiply the specified value.
		/// </summary>
		/// <returns>The multiply.</returns>
		/// <param name="value">Value.</param>
		public DayTime Multiply(float value)
		{
			var dayResult = Day * value;
			var timeResult = dayResult % TimeInDay;
			dayResult -= timeResult;

			timeResult += Time * value;
			dayResult += timeResult - (timeResult % TimeInDay);
			timeResult = timeResult % TimeInDay;

			return new DayTime(Mathf.FloorToInt(dayResult), timeResult);
		}

		/// <summary>
		/// Gets a value from 0.0 to 1.0 representing the progress of this
		/// DayTime between the specified DayTimes.
		/// </summary>
		/// <returns>The normal.</returns>
		/// <param name="dayTime0">Day time0.</param>
		/// <param name="dayTime1">Day time1.</param>
		public float ClampedNormal(DayTime dayTime0, DayTime dayTime1)
		{
			var begin = Min(dayTime0, dayTime1);
			var end = Max(dayTime0, dayTime1);
			if (this <= begin) return 0f;
			if (end <= this) return 1f;
			var delta = Elapsed(begin, end).TotalTime;
			var sinceBegin = Elapsed(begin, this).TotalTime;
			return sinceBegin / delta;
		}

		public static DayTime Max(DayTime dayTime0, DayTime dayTime1)
		{
			if (dayTime0 <= dayTime1) return dayTime1;
			return dayTime0;
		}

		public static DayTime Min(DayTime dayTime0, DayTime dayTime1)
		{
			if (dayTime0 <= dayTime1) return dayTime0;
			return dayTime1;
		}

		public static DayTime Elapsed(DayTime dayTime0, DayTime dayTime1)
		{
			var max = Max(dayTime0, dayTime1);
			var min = Min(dayTime0, dayTime1);
			var day = max.Day - min.Day;
			var time = max.Time - min.Time;
			if (time < 0f)
			{
				time = TimeInDay + time;
				day--;
			}
			return new DayTime(day, time);
		}

		public static DayTime operator +(DayTime obj0, DayTime obj1)
		{
			return obj0.Add(obj1.Day, obj1.Time);
		}

		public static DayTime operator -(DayTime obj0, DayTime obj1)
		{
			return obj0.Add(-obj1.Day, -obj1.Time);
		}

		public static DayTime operator *(DayTime obj, float value)
		{
			return obj.Multiply(value);
		}

		public static DayTime operator *(float value, DayTime obj)
		{
			return obj * value;
		}

		public static bool operator <(DayTime obj0, DayTime obj1)
		{
			if (obj0.Day < obj1.Day) return true;
			if (obj0.Day == obj1.Day && obj0.Time < obj1.Time) return true;
			return false;
		}

		public static bool operator >(DayTime obj0, DayTime obj1)
		{
			if (obj0.Day > obj1.Day) return true;
			if (obj0.Day == obj1.Day && obj0.Time > obj1.Time) return true;
			return false;
		}

		public static bool operator <=(DayTime obj0, DayTime obj1)
		{
			if (obj0 < obj1) return true;
			if (obj0 == obj1) return true;
			return false;
		}

		public static bool operator >=(DayTime obj0, DayTime obj1)
		{
			if (obj0 > obj1) return true;
			if (obj0 == obj1) return true;
			return false;
		}

		public bool Equals(DayTime other)
		{
			return Day == other.Day && Mathf.Approximately(Time, other.Time);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			return obj.GetType() == GetType() && Equals((DayTime)obj);
		}

		public static bool operator ==(DayTime obj0, DayTime obj1)
		{
			if (Equals(obj0, obj1)) return true;
			if (Equals(obj0, null)) return false;
			if (Equals(obj1, null)) return false;
			return obj0.Equals(obj1);
		}

		public static bool operator !=(DayTime obj0, DayTime obj1) { return !(obj0 == obj1); }

		public string ToDayTimeString()
		{
			var totalHours = Time * 24f;
			var daysInYear = Day % DaysInYear;
			var hours = Mathf.Floor(totalHours);
			var minutes = Mathf.Floor(60f * (totalHours - hours));
			return "Year " + Year + " Day " + daysInYear + ", " + hours.ToString("N0").PadLeft(2, '0') + ":" + minutes.ToString("N0").PadLeft(2, '0');
		}

		public override string ToString()
		{
			return "TotalYears: " + TotalYears;
		}
	}
}