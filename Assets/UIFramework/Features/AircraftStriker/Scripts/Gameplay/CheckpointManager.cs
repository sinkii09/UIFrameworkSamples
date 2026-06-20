namespace AircraftStriker
{
    public class CheckpointManager
    {
        private CheckpointState _saved;

        public bool HasCheckpoint    => _saved != null;
        public bool IsPendingRestore { get; private set; }

        public void Save(int waveIndex, PlayerData data) =>
            _saved = CheckpointState.From(waveIndex, data);

        // Called by GameOverViewModel when the player taps "Retry from checkpoint".
        public void RequestRestore() => IsPendingRestore = true;

        // Consumes the restore flag and returns the saved state.
        public CheckpointState ConsumeRestore()
        {
            IsPendingRestore = false;
            return _saved;
        }

        public void Clear()
        {
            _saved           = null;
            IsPendingRestore = false;
        }
    }
}
