﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using TheOracle.BotCore;
using TheOracle.StarForged;

namespace TheOracle.IronSworn
{
    public class PlanetCommands : ModuleBase<SocketCommandContext>
    {
        public const string oneEmoji = "\u0031\u20E3";
        public const string twoEmoji = "\u0032\u20E3";
        public const string threeEmoji = "\u0033\u20E3";

        public PlanetCommands(ServiceProvider services)
        {
            Client = services.GetRequiredService<DiscordSocketClient>();
            var hooks = services.GetRequiredService<HookedEvents>();
            if (!hooks.PlanetReactions)
            {
                hooks.PlanetReactions = true;
                var reactionService = services.GetRequiredService<ReactionService>();

                ReactionEvent reaction1 = new ReactionEventBuilder().WithEmoji(oneEmoji).WithEvent(PlanetReactionHandler).Build();
                ReactionEvent reaction2 = new ReactionEventBuilder().WithEmoji(twoEmoji).WithEvent(PlanetReactionHandler).Build();
                ReactionEvent reaction3 = new ReactionEventBuilder().WithEmoji(threeEmoji).WithEvent(PlanetReactionHandler).Build();

                ReactionEvent look = new ReactionEventBuilder().WithEmoji("🔍").WithEvent(PlanetReactionHandler).Build();
                ReactionEvent Life = new ReactionEventBuilder().WithEmoji("\U0001F996").WithEvent(PlanetReactionHandler).Build();
                ReactionEvent biome = new ReactionEventBuilder().WithEmoji("\uD83C\uDF0D").WithEvent(PlanetReactionHandler).Build();

                reactionService.reactionList.Add(reaction1);
                reactionService.reactionList.Add(reaction2);
                reactionService.reactionList.Add(reaction3);
                reactionService.reactionList.Add(look);
                reactionService.reactionList.Add(Life);
                reactionService.reactionList.Add(biome);
            }
            Services = services;
        }

        public DiscordSocketClient Client { get; }
        public ServiceProvider Services { get; }

        [Command("GeneratePlanet", ignoreExtraArgs: true)]
        [Summary("Creates a template post for a new Starforged planet\n🔍 Adds a Closer Look\n\U0001F996 Reveals any life-forms\n\uD83C\uDF0D Adds a biome (vital worlds only)")]
        [Alias("Planet")]
        public async Task PlanetPost([Remainder] string PlanetCommand = "")
        {
            SpaceRegion spaceRegion = StarforgedUtilites.GetAnySpaceRegion(PlanetCommand);

            string PlanetName = PlanetCommand.Replace(spaceRegion.ToString(), "", StringComparison.OrdinalIgnoreCase).Trim();
            if (PlanetName == string.Empty) PlanetName = $"P-{DateTime.Now.Ticks.ToString().Substring(7)}";

            if (spaceRegion == SpaceRegion.None)
            {
                var palceHolderEmbed = new EmbedBuilder()
                    .WithTitle("__Planet Helper__")
                    .WithDescription(PlanetName)
                    .WithFields(new EmbedFieldBuilder()
                        .WithName("Options:")
                        .WithValue($"{oneEmoji}: Terminus\n{twoEmoji}: Outlands\n{threeEmoji}: Expanse")
                        );

                var msg = await ReplyAsync(embed: palceHolderEmbed.Build());
                await msg.AddReactionAsync(new Emoji(oneEmoji));
                await msg.AddReactionAsync(new Emoji(twoEmoji));
                await msg.AddReactionAsync(new Emoji(threeEmoji));
                return;
            }

            await MakePlanetPost(spaceRegion, PlanetName);
        }

        private async Task MakePlanetPost(SpaceRegion region, string PlanetName, IUserMessage message = null)
        {
            Planet planet = Planet.GeneratePlanet(PlanetName, region, Services);

            if (message != null)
            {
                await message.RemoveAllReactionsAsync();
                await message.ModifyAsync(msg => msg.Embed = planet.GetEmbedBuilder().Build());
            }
            else
            {
                message = await ReplyAsync(embed: planet.GetEmbedBuilder().Build());
            }

            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(new Emoji("🔍"));
                await message.AddReactionAsync(new Emoji("\U0001F996"));

                if (planet.NumberOfBiomes > 1)
                {
                    var biome = new Emoji("\uD83C\uDF0D");
                    await message.AddReactionAsync(biome);
                }
            }).ConfigureAwait(false);
        }

        private async Task Biome(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, IUser user)
        {
            var oldEmbed = message.Embeds.FirstOrDefault();
            var planet = Planet.GeneratePlanetFromEmbed(oldEmbed, Services);

            if (planet.RevealedBiomes >= planet.NumberOfBiomes)
            {
                await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
                return;
            }

            planet.RevealedBiomes++;

            await message.ModifyAsync(msg =>
            {
                msg.Content = string.Empty;
                msg.Embed = planet.GetEmbedBuilder().Build();
            }).ConfigureAwait(false);

            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
            if (planet.RevealedBiomes >= planet.NumberOfBiomes) await message.RemoveReactionAsync(reaction.Emote, message.Author).ConfigureAwait(false);
        }

        private async Task CloserLook(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, IUser user)
        {
            var oldEmbed = message.Embeds.FirstOrDefault();
            var planet = Planet.GeneratePlanetFromEmbed(oldEmbed, Services);

            if (planet.RevealedLooks >= 3)
            {
                await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
                return;
            }

            planet.RevealedLooks++;

            await message.ModifyAsync(msg =>
            {
                msg.Content = string.Empty;
                msg.Embed = planet.GetEmbedBuilder().Build();
            }).ConfigureAwait(false);

            await message.RemoveReactionAsync(reaction.Emote, user).ConfigureAwait(false);
            if (planet.RevealedLooks >= 3) await message.RemoveReactionAsync(reaction.Emote, message.Author).ConfigureAwait(false);
        }

        private async Task Life(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, IUser user)
        {
            var oldEmbed = message.Embeds.FirstOrDefault();
            var planet = Planet.GeneratePlanetFromEmbed(oldEmbed, Services);

            planet.LifeRevealed = true;

            await Task.Run(async () =>
            {
                await message.ModifyAsync(msg =>
                {
                    msg.Content = string.Empty;
                    msg.Embed = planet.GetEmbedBuilder().Build();
                });
                await message.RemoveReactionAsync(reaction.Emote, user);
                await message.RemoveReactionAsync(reaction.Emote, message.Author);
            }).ConfigureAwait(false);
        }

        private async Task PlanetReactionHandler(IUserMessage message, ISocketMessageChannel channel, SocketReaction reaction, IUser user)
        {
            if (reaction.Emote.Name == "🔍") await CloserLook(message, channel, reaction, user).ConfigureAwait(false);
            if (reaction.Emote.Name == "\U0001F996") await Life(message, channel, reaction, user).ConfigureAwait(false);
            if (reaction.Emote.Name == "\uD83C\uDF0D") await Biome(message, channel, reaction, user).ConfigureAwait(false);

            if (message.Embeds.FirstOrDefault()?.Title.Contains("Planet Helper") ?? false)
            {
                string PlanetName = message.Embeds.First().Description;

                if (reaction.Emote.Name == oneEmoji) await MakePlanetPost(SpaceRegion.Terminus, PlanetName, message).ConfigureAwait(false);
                if (reaction.Emote.Name == twoEmoji) await MakePlanetPost(SpaceRegion.Outlands, PlanetName, message).ConfigureAwait(false);
                if (reaction.Emote.Name == threeEmoji) await MakePlanetPost(SpaceRegion.Expanse, PlanetName, message).ConfigureAwait(false);
            }

            return;
        }
    }
}