using System.Collections.Generic;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParserModule.V7_0_3_22248.Parsers
{
    public static class SpellHandler
    {
        public static void ReadSpellCastData(Packet packet, params object[] idx)
        {
            packet.ReadPackedGuid128("CasterGUID", idx);
            packet.ReadPackedGuid128("CasterUnit", idx);

            packet.ReadPackedGuid128("CastID", idx);
            packet.ReadPackedGuid128("OriginalCastID", idx);

            packet.ReadInt32<SpellId>("SpellID", idx);
            packet.ReadUInt32("SpellXSpellVisualID", idx);

            packet.ReadUInt32("CastFlags", idx);
            packet.ReadUInt32("CastTime", idx);

            V6_0_2_19033.Parsers.SpellHandler.ReadMissileTrajectoryResult(packet, idx, "MissileTrajectory");

            packet.ReadInt32("Ammo.DisplayID", idx);

            packet.ReadByte("DestLocSpellCastIndex", idx);

            V6_0_2_19033.Parsers.SpellHandler.ReadCreatureImmunities(packet, idx, "Immunities");

            V6_0_2_19033.Parsers.SpellHandler.ReadSpellHealPrediction(packet, idx, "Predict");

            packet.ResetBitReader();

            packet.ReadBits("CastFlagsEx", 22, idx);
            var hitTargetsCount = packet.ReadBits("HitTargetsCount", 16, idx);
            var missTargetsCount = packet.ReadBits("MissTargetsCount", 16, idx);
            var missStatusCount = packet.ReadBits("MissStatusCount", 16, idx);
            var remainingPowerCount = packet.ReadBits("RemainingPowerCount", 9, idx);

            var hasRuneData = packet.ReadBit("HasRuneData", idx);

            var targetPointsCount = packet.ReadBits("TargetPointsCount", 16, idx);

            for (var i = 0; i < missStatusCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellMissStatus(packet, idx, "MissStatus", i);

            ReadSpellTargetData(packet, idx, "Target");

            for (var i = 0; i < hitTargetsCount; ++i)
                packet.ReadPackedGuid128("HitTarget", idx, i);

            for (var i = 0; i < missTargetsCount; ++i)
                packet.ReadPackedGuid128("MissTarget", idx, i);

            for (var i = 0; i < remainingPowerCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadSpellPowerData(packet, idx, "RemainingPower", i);

            if (hasRuneData)
                V6_0_2_19033.Parsers.SpellHandler.ReadRuneData(packet, idx, "RemainingRunes");

            for (var i = 0; i < targetPointsCount; ++i)
                V6_0_2_19033.Parsers.SpellHandler.ReadLocation(packet, idx, "TargetPoints", i);
        }

        public static void ReadSpellTargetData(Packet packet, params object[] idx)
        {
            packet.ResetBitReader();

            packet.ReadBitsE<TargetFlag>("Flags", 25, idx);
            var hasSrcLoc = packet.ReadBit("HasSrcLocation", idx);
            var hasDstLoc = packet.ReadBit("HasDstLocation", idx);
            var hasOrient = packet.ReadBit("HasOrientation", idx);
            var hasMapID = packet.ReadBit("hasMapID ", idx);
            var nameLength = packet.ReadBits(7);

            packet.ReadPackedGuid128("Unit", idx);
            packet.ReadPackedGuid128("Item", idx);

            if (hasSrcLoc)
                V6_0_2_19033.Parsers.SpellHandler.ReadLocation(packet, "SrcLocation");

            if (hasDstLoc)
                V6_0_2_19033.Parsers.SpellHandler.ReadLocation(packet, "DstLocation");

            if (hasOrient)
                packet.ReadSingle("Orientation", idx);

            if (hasMapID)
                packet.ReadInt32("MapID", idx);

            packet.ReadWoWString("Name", nameLength, idx);
        }

        public static void ReadSandboxScalingData(Packet packet, params object[] idx)
        {
            packet.ReadBits("Type", 3, idx);
            packet.ReadInt16("PlayerLevelDelta", idx);
            packet.ReadByte("TargetLevel", idx);
            packet.ReadByte("Expansion", idx);
            packet.ReadByte("Class", idx);
            packet.ReadByte("TargetMinScalingLevel", idx);
            packet.ReadByte("TargetMaxScalingLevel", idx);
            packet.ReadSByte("TargetScalingLevelDelta", idx);
        }

        public static void ReadSpellCastLogData(Packet packet, params object[] idx)
        {
            packet.ReadInt64("Health", idx);
            packet.ReadInt32("AttackPower", idx);
            packet.ReadInt32("SpellPower", idx);

            var spellLogPowerDataCount = packet.ReadBits("SpellLogPowerData", 9, idx);

            // SpellLogPowerData
            for (var i = 0; i < spellLogPowerDataCount; ++i)
            {
                packet.ReadInt32("PowerType", idx, i);
                packet.ReadInt32("Amount", idx, i);
                packet.ReadInt32("Cost", idx, i);
            }
        }

        [Parser(Opcode.SMSG_SPELL_PREPARE)]
        public static void SpellPrepare(Packet packet)
        {
            packet.ReadPackedGuid128("ClientCastID");
            packet.ReadPackedGuid128("ServerCastID");
        }

        [Parser(Opcode.SMSG_SPELL_START)]
        public static void HandleSpellStart(Packet packet)
        {
            ReadSpellCastData(packet, "Cast");
        }

        [Parser(Opcode.SMSG_SPELL_GO)]
        public static void HandleSpellGo(Packet packet)
        {
            ReadSpellCastData(packet, "Cast");

            packet.ResetBitReader();

            var hasLogData = packet.ReadBit();
            if (hasLogData)
                ReadSpellCastLogData(packet, "LogData");
        }

        [HasSniffData]
        [Parser(Opcode.SMSG_AURA_UPDATE)]
        public static void HandleAuraUpdate(Packet packet)
        {
            packet.ReadBit("UpdateAll");
            var count = packet.ReadBits("AurasCount", 9);

            var auras = new List<Aura>();
            for (var i = 0; i < count; ++i)
            {
                var aura = new Aura();

                packet.ReadByte("Slot", i);

                packet.ResetBitReader();
                var hasAura = packet.ReadBit("HasAura", i);
                if (hasAura)
                {
                    packet.ReadPackedGuid128("CastID");
                    aura.SpellId = (uint)packet.ReadInt32<SpellId>("SpellID", i);
                    packet.ReadInt32("SpellXSpellVisualID", i);
                    aura.AuraFlags = packet.ReadByteE<AuraFlagMoP>("Flags", i);
                    packet.ReadInt32("ActiveFlags", i);
                    aura.Level = packet.ReadUInt16("CastLevel", i);
                    aura.Charges = packet.ReadByte("Applications", i);

                    packet.ResetBitReader();

                    var hasCastUnit = packet.ReadBit("HasCastUnit", i);
                    var hasDuration = packet.ReadBit("HasDuration", i);
                    var hasRemaining = packet.ReadBit("HasRemaining", i);

                    var hasTimeMod = packet.ReadBit("HasTimeMod", i);

                    var pointsCount = packet.ReadBits("PointsCount", 6, i);
                    var effectCount = packet.ReadBits("EstimatedPoints", 6, i);

                    var hasSandboxScaling = packet.ReadBit("HasSandboxScaling", i);

                    if (hasCastUnit)
                        packet.ReadPackedGuid128("CastUnit", i);

                    aura.Duration = hasDuration ? (int)packet.ReadUInt32("Duration", i) : 0;
                    aura.MaxDuration = hasRemaining ? (int)packet.ReadUInt32("Remaining", i) : 0;

                    if (hasTimeMod)
                        packet.ReadSingle("TimeMod");

                    for (var j = 0; j < pointsCount; ++j)
                        packet.ReadSingle("Points", i, j);

                    for (var j = 0; j < effectCount; ++j)
                        packet.ReadSingle("EstimatedPoints", i, j);

                    if (hasSandboxScaling)
                        ReadSandboxScalingData(packet, "SandboxScalingData", i);

                    auras.Add(aura);
                    packet.AddSniffData(StoreNameType.Spell, (int)aura.SpellId, "AURA_UPDATE");
                }
            }

            var guid = packet.ReadPackedGuid128("UnitGUID");

            if (Storage.Objects.ContainsKey(guid))
            {
                var unit = Storage.Objects[guid].Item1 as Unit;
                if (unit != null)
                {
                    // If this is the first packet that sends auras
                    // (hopefully at spawn time) add it to the "Auras" field,
                    // if not create another row of auras in AddedAuras
                    // (similar to ChangedUpdateFields)

                    if (unit.Auras == null)
                        unit.Auras = auras;
                    else
                        unit.AddedAuras.Add(auras);
                }
            }
        }

        [Parser(Opcode.SMSG_CAST_FAILED)]
        public static void HandleCastFailed(Packet packet)
        {
            packet.ReadPackedGuid128("CastID");
            packet.ReadInt32<SpellId>("SpellID");
            packet.ReadInt32<SpellId>("SpellXSpellVisualID");
            packet.ReadInt32("Reason");
            packet.ReadInt32("FailedArg1");
            packet.ReadInt32("FailedArg2");
        }

        [Parser(Opcode.SMSG_PET_CAST_FAILED)]
        public static void HandlePetCastFailed(Packet packet)
        {
            packet.ReadPackedGuid128("CastID");
            packet.ReadInt32<SpellId>("SpellID");
            packet.ReadInt32("Reason");
            packet.ReadInt32("FailedArg1");
            packet.ReadInt32("FailedArg2");
        }

        [Parser(Opcode.SMSG_SPELL_FAILURE)]
        public static void HandleSpellFailure(Packet packet)
        {
            packet.ReadPackedGuid128("CasterUnit");
            packet.ReadPackedGuid128("CastID");
            packet.ReadInt32<SpellId>("SpellID");
            packet.ReadUInt32("SpellXSpellVisualID");
            packet.ReadInt16E<SpellCastFailureReason>("Reason");
        }

        [Parser(Opcode.SMSG_SPELL_FAILED_OTHER)]
        public static void HandleSpellFailedOther(Packet packet)
        {
            packet.ReadPackedGuid128("CasterUnit");
            packet.ReadPackedGuid128("CastID");
            packet.ReadUInt32<SpellId>("SpellID");
            packet.ReadUInt32("SpellXSpellVisualID");
            packet.ReadByteE<SpellCastFailureReason>("Reason");
        }
    }
}
