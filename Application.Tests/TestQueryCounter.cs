using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Application.Tests
{
    /// <summary>
    /// Captures every SQL command issued through the DbContext so tests can assert
    /// on query shapes and counts (e.g. no N+1 product queries).
    /// </summary>
    public sealed class TestQueryCounter : DbCommandInterceptor
    {
        private readonly object _lock = new();
        private readonly List<string> _commands = new();

        public void Reset()
        {
            lock (_lock)
            {
                _commands.Clear();
            }
        }

        public int Total
        {
            get
            {
                lock (_lock)
                {
                    return _commands.Count;
                }
            }
        }

        public IReadOnlyList<string> Commands
        {
            get
            {
                lock (_lock)
                {
                    return _commands.ToArray();
                }
            }
        }

        public int SelectCount(string tableToken)
        {
            lock (_lock)
            {
                return _commands.Count(c => IsSelect(c) && c.Contains(tableToken, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        public static bool IsSelect(string text)
            => text.StartsWith("SELECT", System.StringComparison.OrdinalIgnoreCase);

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Record(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Record(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void Record(DbCommand command)
        {
            lock (_lock)
            {
                _commands.Add(command.CommandText);
            }
        }
    }
}