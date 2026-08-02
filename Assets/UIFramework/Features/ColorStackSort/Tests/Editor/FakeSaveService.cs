using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using Sinkii09.UIFramework;

namespace ColorStackSort.Tests
{
    /// <summary>
    /// In-memory <see cref="ISaveService"/> that can be told to fail in each way the real one does.
    /// <para>
    /// <b>Fully synchronous</b> — every method returns an already-completed UniTask, mirroring the
    /// framework's own <c>FakeStorageBackend</c>. No <c>Yield</c> or <c>Delay</c>: EditMode has no
    /// player loop pumping continuations, so an awaited yield here would hang the test run rather
    /// than fail it.
    /// </para>
    /// </summary>
    internal sealed class FakeSaveService : ISaveService
    {
        internal enum LoadBehaviour
        {
            /// <summary>No file yet — the service's only null-returning path.</summary>
            Missing,
            ReturnsStored,
            /// <summary>Present but unreadable, past backup recovery.</summary>
            Corrupt,
            /// <summary>Written by a newer build. Must never be overwritten.</summary>
            NewerSchema
        }

        internal LoadBehaviour Behaviour = LoadBehaviour.Missing;
        internal ColorStackSortSaveData Stored;
        internal bool ThrowOnSave;

        /// <summary>Counts writes, so a test can assert that none happened.</summary>
        internal int SaveCallCount { get; private set; }

        internal ColorStackSortSaveData LastSaved { get; private set; }

        internal CancellationToken LastSaveToken { get; private set; }

        public UniTask SaveAsync<T>(T data, CancellationToken ct = default) where T : class
            => SaveAsync(typeof(T).Name, data, ct);

        public UniTask SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : class
        {
            if (!IsValidKey(key))
                return UniTask.FromException(KeyRejected(key));

            SaveCallCount++;
            LastSaveToken = ct;

            if (ThrowOnSave)
                return UniTask.FromException(new InvalidOperationException("disk full (simulated)"));

            // Captured by reference on purpose: the service under test is expected to hold ONE
            // mutable instance, and a test asserts that the object it handed us reflects later
            // mutations rather than being a stale snapshot.
            LastSaved = data as ColorStackSortSaveData;
            Stored = LastSaved;
            return UniTask.CompletedTask;
        }

        public UniTask<T> LoadAsync<T>(CancellationToken ct = default) where T : class
            => LoadAsync<T>(typeof(T).Name, ct);

        public UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            // Both of these mirror JsonSaveService, and both are load-bearing rather than
            // decorative. Without the key check a malformed SaveKey keeps every test green while
            // production degrades to "load fails, saving stays enabled, every write faults"; without
            // the token check a cancelled load would return normally here and ENABLE saving, the
            // exact inverse of the real service (LocalFileStorageBackend passes ct to
            // RunOnThreadPool and throws inside it).
            if (!IsValidKey(key))
                return UniTask.FromException<T>(KeyRejected(key));

            if (ct.IsCancellationRequested)
                return UniTask.FromCanceled<T>(ct);

            switch (Behaviour)
            {
                case LoadBehaviour.Corrupt:
                    return UniTask.FromException<T>(
                        new InvalidOperationException("save file is malformed (simulated)"));

                case LoadBehaviour.NewerSchema:
                    return UniTask.FromException<T>(new SaveSchemaVersionException(key, 99, 1));

                case LoadBehaviour.ReturnsStored:
                    return UniTask.FromResult(Stored as T);

                default:
                    return UniTask.FromResult<T>(null);
            }
        }

        public UniTask<bool> ExistsAsync<T>(CancellationToken ct = default) where T : class
            => UniTask.FromResult(Stored != null);

        public UniTask<bool> ExistsAsync(string key, CancellationToken ct = default)
            => UniTask.FromResult(Stored != null);

        public UniTask<bool> DeleteAsync<T>(CancellationToken ct = default) where T : class
            => DeleteAsync(typeof(T).Name, ct);

        public UniTask<bool> DeleteAsync(string key, CancellationToken ct = default)
        {
            var had = Stored != null;
            Stored = null;
            return UniTask.FromResult(had);
        }

        /// <summary>Copy of <c>JsonSaveService.KeyPattern</c> — the key IS the on-disk filename.</summary>
        private static bool IsValidKey(string key)
            => !string.IsNullOrEmpty(key) && Regex.IsMatch(key, "^[A-Za-z0-9_-]+$");

        private static ArgumentException KeyRejected(string key)
            => new($"Save key '{key}' must match ^[A-Za-z0-9_-]+$.", nameof(key));

        public Observable<SaveEventArgs> OnSaveStartedAsObservable => Observable.Empty<SaveEventArgs>();
        public Observable<SaveEventArgs> OnSaveCompletedAsObservable => Observable.Empty<SaveEventArgs>();
        public Observable<SaveEventArgs> OnSaveFailedAsObservable => Observable.Empty<SaveEventArgs>();
    }
}
