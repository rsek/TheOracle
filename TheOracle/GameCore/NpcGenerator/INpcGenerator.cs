﻿using Discord;

namespace TheOracle.GameCore.NpcGenerator
{
    public interface INpcGenerator
    {
        public Embed GetEmbed();

        public void BuildNPCFromEmbed(Embed embed);

        public INpcGenerator Build(string NPCCreationOptions);
    }
}