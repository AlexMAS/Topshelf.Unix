using System;
using System.Collections.Generic;
using System.Linq;

using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
	internal sealed class TransactionManager<TContext>
	{
		public delegate void LogFunc(string format, params object[] args);


		public TransactionManager(LogFunc logFunc = null)
		{
			_logFunc = logFunc;
			_stages = new List<StageInfo>();
		}


		private readonly LogFunc _logFunc;
		private readonly List<StageInfo> _stages;


		public TransactionManager<TContext> Stage(string name, Action<TContext> execute, Action<TContext> rollback = null)
		{
			_stages.Add(new StageInfo(name, execute, rollback));

			return this;
		}


		public void Execute(TContext context)
		{
			var rollbackPath = new Stack<StageInfo>();

			foreach (var stage in _stages)
			{
				rollbackPath.Push(stage);

				WriteLog(Resources.ExecutingStageIsStarted, stage);

				try
				{
					stage.Execute(context);

					WriteLog(Resources.ExecutingStageIsSuccessfullyCompleted, stage);
				}
				catch (Exception error)
				{
					WriteLog(Resources.ExecutingStageIsCompletedWithErrors, stage, error);

					var rollbackErrors = Rollback(context, rollbackPath);

					throw new AggregateException(Resources.ExecutingTransactionFailed, new[] { error }.Concat(rollbackErrors));
				}
			}
		}


		public void Rollback(TContext context)
		{
			var rollbackErrors = Rollback(context, Enumerable.Reverse(_stages));

			if (rollbackErrors.Count > 0)
			{
				throw new AggregateException(Resources.ExecutingRollbackTransactionFailed, rollbackErrors);
			}
		}


		private List<Exception> Rollback(TContext context, IEnumerable<StageInfo> rollbackPath)
		{
			var errors = new List<Exception>();

			foreach (var stage in rollbackPath)
			{
				WriteLog(Resources.RollbackStageIsStarted, stage);

				try
				{
					stage.Rollback(context);

					WriteLog(Resources.RollbackStageIsSuccessfullyCompleted, stage);
				}
				catch (Exception error)
				{
					WriteLog(Resources.RollbackStageIsCompletedWithErrors, stage, error);

					errors.Add(error);
				}
			}

			return errors;
		}

		private void WriteLog(string format, params object[] args)
		{
			if (_logFunc != null)
			{
				try
				{
					_logFunc(format, args);
				}
				catch
				{
				}
			}
		}


		internal class StageInfo
		{
			public StageInfo(string name, Action<TContext> execute, Action<TContext> rollback)
			{
				_name = name;
				_execute = execute;
				_rollback = rollback;
			}


			private readonly string _name;
			private readonly Action<TContext> _execute;
			private readonly Action<TContext> _rollback;


			public string Name
			{
				get
				{
					return _name;
				}
			}

			public void Execute(TContext context)
			{
				if (_execute != null)
				{
					try
					{
						_execute(context);
					}
					catch (Exception error)
					{
						throw new InvalidOperationException(string.Format(Resources.CantExecuteStage, _name), error);
					}
				}
			}

			public void Rollback(TContext context)
			{
				if (_rollback != null)
				{
					try
					{
						_rollback(context);
					}
					catch (Exception error)
					{
						throw new InvalidOperationException(string.Format(Resources.CantRollbackStage, _name), error);
					}
				}
			}

			public override string ToString()
			{
				return Name;
			}
		}
	}
}