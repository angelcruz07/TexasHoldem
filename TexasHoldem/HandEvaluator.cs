using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexasHoldem
{
    internal class HandEvaluator : IComparable<HandEvaluator>
    {
        public enum HandRank
        {
            HighCard = 1,        // Carta Alta
            Pair = 2,            // Par
            TwoPair = 3,         // Dos Pares
            ThreeOfAKind = 4,    // Trío
            Straight = 5,        // Escalera
            Flush = 6,           // Color
            FullHouse = 7,       // Full
            FourOfAKind = 8,     // Póker
            StraightFlush = 9    // Escalera de Color
        }
        public HandRank Rank { get; private set; }

        public List<Rank> PrimaryRanks { get; private set; }

        // Propiedad 3: Los kickers, usados solo para desempate.
        public List<Rank> KickerRanks { get; private set; }

        public HandEvaluator(HandRank rank, List<Rank> primaryRanks, List<Rank> kickerRanks)
        {
            this.Rank = rank;
            this.PrimaryRanks = primaryRanks;
            this.KickerRanks = kickerRanks;
        }

        // Método estático para evaluar una mano de 7 cartas (2 hole cards + 5 community cards)
        public static HandEvaluator EvaluateHand(List<Card> holeCards, List<Card> communityCards)
        {
            List<Card> allCards = new List<Card>(holeCards);
            allCards.AddRange(communityCards);

            // En Texas Hold'em, se usan 5 de las 7 cartas para formar la mejor mano
            return EvaluateBestHand(allCards);
        }

        private static HandEvaluator EvaluateBestHand(List<Card> cards)
        {
            // Generar todas las combinaciones posibles de 5 cartas de las 7 disponibles
            var combinations = GetCombinations(cards, 5);
            HandEvaluator bestHand = null;

            foreach (var combo in combinations)
            {
                HandEvaluator hand = EvaluateFiveCards(combo);
                if (bestHand == null || hand.CompareTo(bestHand) > 0)
                {
                    bestHand = hand;
                }
            }

            return bestHand;
        }

        private static List<List<Card>> GetCombinations(List<Card> cards, int k)
        {
            List<List<Card>> result = new List<List<Card>>();
            GetCombinationsRecursive(cards, k, 0, new List<Card>(), result);
            return result;
        }

        private static void GetCombinationsRecursive(List<Card> cards, int k, int start, List<Card> current, List<List<Card>> result)
        {
            if (current.Count == k)
            {
                result.Add(new List<Card>(current));
                return;
            }

            for (int i = start; i < cards.Count; i++)
            {
                current.Add(cards[i]);
                GetCombinationsRecursive(cards, k, i + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private static HandEvaluator EvaluateFiveCards(List<Card> cards)
        {
            // Ordenar cartas por rango
            var sortedCards = cards.OrderByDescending(c => c.Rank).ToList();

            // Verificar Straight Flush
            var straightFlush = CheckStraightFlush(sortedCards);
            if (straightFlush != null) return straightFlush;

            // Verificar Four of a Kind
            var fourOfAKind = CheckFourOfAKind(sortedCards);
            if (fourOfAKind != null) return fourOfAKind;

            // Verificar Full House
            var fullHouse = CheckFullHouse(sortedCards);
            if (fullHouse != null) return fullHouse;

            // Verificar Flush
            var flush = CheckFlush(sortedCards);
            if (flush != null) return flush;

            // Verificar Straight
            var straight = CheckStraight(sortedCards);
            if (straight != null) return straight;

            // Verificar Three of a Kind
            var threeOfAKind = CheckThreeOfAKind(sortedCards);
            if (threeOfAKind != null) return threeOfAKind;

            // Verificar Two Pair
            var twoPair = CheckTwoPair(sortedCards);
            if (twoPair != null) return twoPair;

            // Verificar Pair
            var pair = CheckPair(sortedCards);
            if (pair != null) return pair;

            // High Card
            return new HandEvaluator(HandRank.HighCard, 
                new List<Rank>(), 
                sortedCards.Select(c => c.Rank).ToList());
        }

        private static HandEvaluator CheckStraightFlush(List<Card> cards)
        {
            var flush = CheckFlush(cards);
            if (flush == null) return null;

            var straight = CheckStraight(cards);
            if (straight != null)
            {
                return new HandEvaluator(HandRank.StraightFlush, 
                    straight.PrimaryRanks, 
                    new List<Rank>());
            }
            return null;
        }

        private static HandEvaluator CheckFourOfAKind(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).Where(g => g.Count() == 4);
            if (groups.Any())
            {
                var fourRank = groups.First().Key;
                var kicker = cards.First(c => c.Rank != fourRank).Rank;
                return new HandEvaluator(HandRank.FourOfAKind, 
                    new List<Rank> { fourRank }, 
                    new List<Rank> { kicker });
            }
            return null;
        }

        private static HandEvaluator CheckFullHouse(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).ToList();
            var three = groups.FirstOrDefault(g => g.Count() == 3);
            var pair = groups.FirstOrDefault(g => g.Count() == 2);

            if (three != null && pair != null)
            {
                return new HandEvaluator(HandRank.FullHouse, 
                    new List<Rank> { three.Key, pair.Key }, 
                    new List<Rank>());
            }
            return null;
        }

        private static HandEvaluator CheckFlush(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Suit);
            if (groups.Any(g => g.Count() >= 5))
            {
                var flushCards = groups.First(g => g.Count() >= 5).OrderByDescending(c => c.Rank).Take(5).ToList();
                return new HandEvaluator(HandRank.Flush, 
                    new List<Rank>(), 
                    flushCards.Select(c => c.Rank).ToList());
            }
            return null;
        }

        private static HandEvaluator CheckStraight(List<Card> cards)
        {
            var ranks = cards.Select(c => (int)c.Rank).Distinct().OrderByDescending(r => r).ToList();
            
            // Verificar straight normal
            for (int i = 0; i <= ranks.Count - 5; i++)
            {
                bool isStraight = true;
                for (int j = 1; j < 5; j++)
                {
                    if (ranks[i + j] != ranks[i] - j)
                    {
                        isStraight = false;
                        break;
                    }
                }
                if (isStraight)
                {
                    return new HandEvaluator(HandRank.Straight, 
                        new List<Rank> { (Rank)ranks[i] }, 
                        new List<Rank>());
                }
            }

            // Verificar straight con A-2-3-4-5 (wheel)
            if (ranks.Contains(14) && ranks.Contains(2) && ranks.Contains(3) && ranks.Contains(4) && ranks.Contains(5))
            {
                return new HandEvaluator(HandRank.Straight, 
                    new List<Rank> { (Rank)5 }, 
                    new List<Rank>());
            }

            return null;
        }

        private static HandEvaluator CheckThreeOfAKind(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).Where(g => g.Count() == 3);
            if (groups.Any())
            {
                var threeRank = groups.First().Key;
                var kickers = cards.Where(c => c.Rank != threeRank)
                    .OrderByDescending(c => c.Rank)
                    .Take(2)
                    .Select(c => c.Rank)
                    .ToList();
                return new HandEvaluator(HandRank.ThreeOfAKind, 
                    new List<Rank> { threeRank }, 
                    kickers);
            }
            return null;
        }

        private static HandEvaluator CheckTwoPair(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).Where(g => g.Count() == 2).OrderByDescending(g => g.Key).ToList();
            if (groups.Count >= 2)
            {
                var pair1 = groups[0].Key;
                var pair2 = groups[1].Key;
                var kicker = cards.First(c => c.Rank != pair1 && c.Rank != pair2).Rank;
                return new HandEvaluator(HandRank.TwoPair, 
                    new List<Rank> { pair1, pair2 }, 
                    new List<Rank> { kicker });
            }
            return null;
        }

        private static HandEvaluator CheckPair(List<Card> cards)
        {
            var groups = cards.GroupBy(c => c.Rank).Where(g => g.Count() == 2);
            if (groups.Any())
            {
                var pairRank = groups.First().Key;
                var kickers = cards.Where(c => c.Rank != pairRank)
                    .OrderByDescending(c => c.Rank)
                    .Take(3)
                    .Select(c => c.Rank)
                    .ToList();
                return new HandEvaluator(HandRank.Pair, 
                    new List<Rank> { pairRank }, 
                    kickers);
            }
            return null;
        }

        public int CompareTo(HandEvaluator other)
        {
            if (other == null) return 1;

            // 1. Comparar por Rank (Ej: Full House vs Flush)
            int rankComparison = this.Rank.CompareTo(other.Rank);
            if (rankComparison != 0)
            {
                return rankComparison;
            }

            // 2. Si son iguales, comparar PrimaryRanks (Ej: Full de Ases vs Full de Reyes)
            int maxPrimary = Math.Max(this.PrimaryRanks.Count, other.PrimaryRanks.Count);
            for (int i = 0; i < maxPrimary; i++)
            {
                if (i >= this.PrimaryRanks.Count) return -1;
                if (i >= other.PrimaryRanks.Count) return 1;
                
                int primaryComparison = this.PrimaryRanks[i].CompareTo(other.PrimaryRanks[i]);
                if (primaryComparison != 0)
                {
                    return primaryComparison;
                }
            }

            // 3. Si siguen empatados, comparar KickerRanks
            int maxKicker = Math.Max(this.KickerRanks.Count, other.KickerRanks.Count);
            for (int i = 0; i < maxKicker; i++)
            {
                if (i >= this.KickerRanks.Count) return -1;
                if (i >= other.KickerRanks.Count) return 1;
                
                int kickerComparison = this.KickerRanks[i].CompareTo(other.KickerRanks[i]);
                if (kickerComparison != 0)
                {
                    return kickerComparison;
                }
            }

            return 0; // Empate completo
        }

        public string GetHandName()
        {
            switch (Rank)
            {
                case HandRank.StraightFlush: return "Escalera de Color";
                case HandRank.FourOfAKind: return "Póker";
                case HandRank.FullHouse: return "Full House";
                case HandRank.Flush: return "Color";
                case HandRank.Straight: return "Escalera";
                case HandRank.ThreeOfAKind: return "Trío";
                case HandRank.TwoPair: return "Dos Pares";
                case HandRank.Pair: return "Par";
                case HandRank.HighCard: return "Carta Alta";
                default: return "Desconocido";
            }
        }
    }
}
