﻿using System;
using System.Collections.Generic;
using System.Linq;
using PoolScoreKeeper.Interfaces;
using PoolScoreKeeper.Models;
using Raven.Client;

namespace PoolScoreKeeper.Services
{
    public class PoolOperations : IPoolOperations
    {
        private readonly IDocumentStore store = RavenDb.GetStore();

        public ScoreBoard GetScoreBoard()
        {
            List<Player> players;
            int[,] scores;
            using (var session = store.OpenSession())
            {
                players = session.Query<Player>().Customize(x => x.WaitForNonStaleResultsAsOfNow(TimeSpan.FromSeconds(5))).OrderBy(x => x.FirstName).ToList();
                List<PoolGame> poolGames = session.Query<PoolGame>().Take(10000).ToList();

                scores = new int[players.Count, players.Count];
                for (int i = 0; i < players.Count; i++)
                {
                    for (int j = 0; j < players.Count; j++)
                    {
                        if (i == j)
                            continue;

                        scores[i, j] = poolGames.Count(x => x.WinnerId == players[i].Id && x.LoserId == players[j].Id);
                    }
                }
            }

            return new ScoreBoard
            {
                Players = players,
                Scores = scores
            };
        }

        public PlayerStatistics GetPlayerStatistics(string playerId)
        {
            using (var session = store.OpenSession())
            {
                var player = session.Load<Player>(playerId);
                var poolGames = session.Query<PoolGame>()
                    .Where(x => x.LoserId == playerId || x.WinnerId == playerId).ToList();

                var losses = poolGames.Count(x => x.LoserId == playerId);
                var wins = poolGames.Count(x => x.WinnerId == playerId);
                var winningEightBalls = poolGames.Count(x => x.WinnerId == playerId && x.WinnerPocketedEightball);
                var losingEightBalls = poolGames.Count(x => x.LoserId == playerId && !x.WinnerPocketedEightball);

                return new PlayerStatistics
                {
                    Id = playerId,
                    Name = player.FirstName + " " + player.LastName,
                    Losses = losses,
                    Wins = wins,
                    WinningEightBalls = winningEightBalls,
                    LosingEightBalls = losingEightBalls
                };
            }
        }

        public void RegisterGame(PoolGame poolGame)
        {
            poolGame.CreatedDate = DateTime.Now;
            using (var session = store.OpenSession())
            {
                session.Store(poolGame);
                session.SaveChanges();
            }
        }

        public void AddPlayer(Player player)
        {
            using (var session = store.OpenSession())
            {
                
                session.Store(player);
                session.SaveChanges();
            }
        }

        private Player GetPlayer(string name)
        {
            var splittedName = name.Split('-');
            var firstName = splittedName[0];
            var lastName = splittedName[1];

            using (var session = store.OpenSession())
            {
                return session.Query<Player>().First(x => x.FirstName  == firstName && x.LastName == lastName);
            }
        }
        public ComparePlayersStatistics GetComparePlayersStatistics(string winnerSidePlayerName, string runnerUpSidePlayerName)
        {
            using (var session = store.OpenSession())
            {
                var winnerSidePlayer = GetPlayer(winnerSidePlayerName);
                var runnerUpSidePlayer = GetPlayer(runnerUpSidePlayerName);
                var poolGames = session.Query<PoolGame>().Where(x => (x.WinnerId == winnerSidePlayer.Id || x.WinnerId == runnerUpSidePlayer.Id) && 
                                                                     (x.LoserId == winnerSidePlayer.Id || x.LoserId == runnerUpSidePlayer.Id)).ToList();

                var winnerSideWins = poolGames.Count(x => x.WinnerId == winnerSidePlayer.Id);
                var runnerUpSideWins = poolGames.Count(x => x.WinnerId == runnerUpSidePlayer.Id);
                var winnerSideWinningEightBalls = poolGames.Count(x => x.WinnerId == winnerSidePlayer.Id && x.WinnerPocketedEightball);
                var winnerSideLosingEightBalls = poolGames.Count(x => x.LoserId == winnerSidePlayer.Id && !x.WinnerPocketedEightball);
                var runnerUpSideWinningEightBalls = poolGames.Count(x => x.WinnerId == runnerUpSidePlayer.Id && x.WinnerPocketedEightball);
                var runnerUpSideLosingEightBalls = poolGames.Count(x => x.LoserId == runnerUpSidePlayer.Id && !x.WinnerPocketedEightball);

                return new ComparePlayersStatistics
                {
                    WinningSidePlayer = CreatePlayerStatistics(winnerSidePlayer, winnerSideWins, runnerUpSideWins, winnerSideWinningEightBalls, winnerSideLosingEightBalls),
                    RunnerUpSidePlayer = CreatePlayerStatistics(runnerUpSidePlayer, runnerUpSideWins, winnerSideWins, runnerUpSideWinningEightBalls, runnerUpSideLosingEightBalls)
                };
            }
        }

        private static PlayerStatistics CreatePlayerStatistics(Player player, int wins, int losses, int winningEightBalls, int losingEightBalls)
        {
            return new PlayerStatistics
            {
                Id = player.Id,
                Name = player.FirstName + " " + player.LastName,
                Wins = wins,
                Losses = losses,
                WinningEightBalls = winningEightBalls,
                LosingEightBalls = losingEightBalls
            };
        }
    }
}