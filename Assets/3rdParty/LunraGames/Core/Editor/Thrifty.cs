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


		static List<Entry> actions = new List<Entry>();

		static bool running;
	    static Thread thread;
	    static Entry threadedLambda;
	    static Exception threadedException;

		[InitializeOnLoadMethod]
		static void Initialize()
		{
			EditorApplication.update += Update;
		}

		static void Update()
		{
			if (EditorApplication.isCompiling || actions == null || actions.Count == 0) return;

			if (!running)
			{
				if (threadedException != null) Debug.LogException(threadedException);
				if (threadedLambda != null) 
				{
					if (threadedException != null)
					{
						threadedLambda.Errored?.Invoke(threadedException);
					}
					else
					{
						threadedLambda.Completed?.Invoke();
					}

					actions.Remove(threadedLambda);
				}

				threadedLambda = actions.FirstOrDefault();
				thread = new Thread(ThreadedWork);
				thread.Start();
			}
		}

	    public static void Queue(Action threaded, Action completed = null, Action<Exception> errored = null)
	    {
	    	if (threaded == null) throw new ArgumentNullException(nameof(threaded));
	    	actions.Add(new Entry { Threaded = threaded, Completed = completed, Errored = errored});
	    }

	    static void ThreadedWork()
	    {
	        running = true;
	        threadedException = null;
	        var isDone = false;

	        // This pattern lets us interrupt the work at a safe point if needed.
	        while(running && !isDone)
	        {
				try
				{
					threadedLambda?.Threaded();
				}
            	catch (Exception e) 
            	{ 
            		threadedException = e;
        		}

	            isDone = true;
	        }
	        running = false;
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