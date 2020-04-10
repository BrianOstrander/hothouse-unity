using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;

namespace LunraGamesEditor
{
	public static class Thrifty 
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
	}
}