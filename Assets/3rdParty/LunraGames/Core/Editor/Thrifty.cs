using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;

namespace LunraGamesEditor
{
	public class Thrifty 
	{
		class Entry 
		{
			public Action Threaded;
			public Action Completed;
			public Action<Exception> Errored;
		}


		static List<Entry> Actions = new List<Entry>();

		static bool Running;
	    static Thread Thread;
	    static Entry ThreadedLambda;
	    static Exception ThreadedException;

		[InitializeOnLoadMethod]
		static void Initialize()
		{
			EditorApplication.update += Update;
		}

		static void Update()
		{
			if (EditorApplication.isCompiling || Actions == null || Actions.Count == 0) return;

			if (!Running)
			{
				if (ThreadedException != null) Debug.LogException(ThreadedException);
				if (ThreadedLambda != null) 
				{
					if (ThreadedException != null)
					{
						if (ThreadedLambda.Errored != null) ThreadedLambda.Errored(ThreadedException);
					}
					else if (ThreadedLambda.Completed != null) ThreadedLambda.Completed();

					Actions.Remove(ThreadedLambda);
				}

				ThreadedLambda = Actions.FirstOrDefault();
				Thread = new Thread(ThreadedWork);
				Thread.Start();
			}
		}

	    public static void Queue(Action threaded, Action completed = null, Action<Exception> errored = null)
	    {
	    	if (threaded == null) throw new ArgumentNullException("threaded");
	    	Actions.Add(new Entry { Threaded = threaded, Completed = completed, Errored = errored});
	    }

	    static void ThreadedWork()
	    {
	        Running = true;
	        ThreadedException = null;
	        var isDone = false;

	        // This pattern lets us interrupt the work at a safe point if neeeded.
	        while(Running && !isDone)
	        {
				try 
	            {
	            	if (ThreadedLambda != null) ThreadedLambda.Threaded();
            	}
            	catch (Exception e) 
            	{ 
            		ThreadedException = e;
        		}

	            isDone = true;
	        }
	        Running = false;
	    }
	    /*
	    void OnDisabled()
	    {
	        // If the thread is still running, we should shut it down,
	        // otherwise it can prevent the game from exiting correctly.
	        if(_threadRunning)
	        {
	            // This forces the while loop in the ThreadedWork function to abort.
	            _threadRunning = false;

	            // This waits until the thread exits,
	            // ensuring any cleanup we do after this is safe. 
	            _thread.Join();
	        }

	        // Thread is guaranteed no longer running. Do other cleanup tasks.
	    }
	    */
	/*
		const int MillisecondBudget = 20;

		static List<Func<bool>> Entries = new List<Func<bool>>();

		[InitializeOnLoadMethod]
		static void Initialize()
		{
			EditorApplication.update += Farm;
		}

		static void Farm()
		{
			if (Entries == null || Entries.Count == 0) return;

			var remainingBudget = PixelBudget;
			var deletions = new List<Func<bool>>();

			foreach (var entry in Entries)
			{
				remainingBudget -= entry.Target.width;

				var pixels = new Color[entry.Target.width];
				var start = entry.YProgress * entry.Target.width;
				var end = start + entry.Target.width;

				for (var i = start; i < end; i++) pixels[i - start] = entry.Replacements[i];

				entry.Target.SetPixels(0, entry.YProgress, entry.Target.width, 1, pixels);
				entry.Target.Apply();

				entry.YProgress++;

				if (entry.YProgress == entry.Target.height) deletions.Add(entry);

				if (remainingBudget <= 0) break;
			}

			foreach (var deletion in deletions) Entries.Remove(deletion);
		}

		public static void Queue(Texture2D target, Color[] replacements)
		{
			var entry = Entries.FirstOrDefault(e => target == e.Target);

			if (entry == null)
			{
				entry = new Entry {
					Target = target,
					Replacements = replacements
				};
				Entries.Add(entry);
			}
			else
			{
				entry.Replacements = replacements;
				//entry.XProgress = 0;
				entry.YProgress = 0;
			}
		}
		*/
	}
}