using System;
using System.Collections.Generic;

namespace MemoryGame
{
    public class MemoryCardGame
    {
        public IReadOnlyList<CardData> Cards => _cards;
        public int Moves { get; private set; }
        public int MatchesFound { get; private set; }
        public int TotalPairs { get; private set; }
        public bool IsComplete => MatchesFound == TotalPairs;

        public event Action<int, int> OnMatchFound;   // (cardId1, cardId2)
        public event Action<int, int> OnMismatch;     // (cardId1, cardId2)
        public event Action OnGameComplete;

        private CardData[] _cards;
        private int _firstFlippedId = -1;
        private bool _locked;

        public void Initialize(int pairs)
        {
            TotalPairs = pairs;
            _firstFlippedId = -1;
            _locked = false;
            Moves = 0;
            MatchesFound = 0;
            _cards = BuildShuffledDeck(pairs);
        }

        public FlipResult TryFlip(int cardId)
        {
            if (_locked) return FlipResult.Locked;
            ref var card = ref _cards[cardId];
            if (card.IsMatched) return FlipResult.AlreadyMatched;
            if (card.IsFlipped) return FlipResult.AlreadyFlipped;

            card.IsFlipped = true;

            if (_firstFlippedId == -1)
            {
                _firstFlippedId = cardId;
                return FlipResult.NeedSecond;
            }

            Moves++;
            int firstId = _firstFlippedId;
            _firstFlippedId = -1;

            if (_cards[firstId].PairId == card.PairId)
            {
                _cards[firstId].IsMatched = true;
                card.IsMatched = true;
                MatchesFound++;
                OnMatchFound?.Invoke(firstId, cardId);
                if (IsComplete) OnGameComplete?.Invoke();
                return FlipResult.Match;
            }

            _locked = true;
            OnMismatch?.Invoke(firstId, cardId);
            return FlipResult.Mismatch;
        }

        // Called by ViewModel after the mismatch reveal delay; flips both cards back and unlocks.
        public void ResolveMismatch(int id1, int id2)
        {
            _cards[id1].IsFlipped = false;
            _cards[id2].IsFlipped = false;
            _locked = false;
        }

        private static CardData[] BuildShuffledDeck(int pairs)
        {
            int total = pairs * 2;
            var deck = new CardData[total];
            for (int i = 0; i < total; i++)
                deck[i] = new CardData { Id = i, PairId = i / 2, FaceIndex = i / 2 };

            // Fisher-Yates shuffle — Id is re-stamped after each swap so slot index stays correct.
            var rng = new System.Random();
            for (int i = total - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
                deck[i].Id = i;
                deck[j].Id = j;
            }
            return deck;
        }
    }
}
