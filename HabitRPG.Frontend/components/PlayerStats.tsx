import React, { useState, useEffect } from "react"
import { View, Text, StyleSheet, ActivityIndicator } from "react-native"
import { Ionicons } from "@expo/vector-icons"
import XPProgressBar from "./XPProgressBar"
import { colors, spacing, borderRadius, fontSize } from "../constants/theme"
import { Crown, Zap, Flame, Target } from "lucide-react-native"
import api from "../api/api"
import { UserProfile, ApiError } from "../types/types"
import { useAuth } from "../context/AuthContext"
import { PlayerStatsSkeleton } from "./skeletons"

interface PlayerStatsProps {
    userId?: number;
    refreshTrigger?: number;
}

export default function PlayerStats({ userId, refreshTrigger }: PlayerStatsProps) {
  const { user } = useAuth();
  const [playerData, setPlayerData] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPlayerProfile = async () => {
    if (!user) {
        setIsLoading(false);
        return;
    }

    try {
      setIsLoading(true);
      setError(null);

      console.log("🚀 Fetching player profile...");
      const response = await api.get("/user/profile");
      const profile: UserProfile = response.data;

      console.log("✅ Player profile loaded successfully");
      setPlayerData(profile);
    } catch (error) {
      console.error("❌ Error fetching player profile:", error);
      const err = error as ApiError;

      if (!user)
          return;
    
      setError(err.message || "Failed to load player data");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (user)
      fetchPlayerProfile();
    else {
        setPlayerData(null);
        setIsLoading(false);
        setError(null);
    }
  }, [user, userId, refreshTrigger]);

  if (isLoading)
    return <PlayerStatsSkeleton />;

  if (error || !playerData)
    return (
      <View style={styles.container}>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error || "Failed to load player data"}</Text>
        </View>
      </View>
    );

  const xpProgress = playerData.xpRequiredForNextLevel > 0 
    ? (playerData.xpProgress / playerData.xpRequiredForNextLevel) * 100
    : 0;

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View style={styles.avatarContainer}>
          <View style={styles.avatar}>
            <Crown size={32} color={colors.gold} />
          </View>
          <View style={styles.levelBadge}>
            <Text style={styles.levelText}>{playerData.level}</Text>
          </View>
        </View>
        <View style={styles.playerInfo}>
          <Text style={styles.levelTitle}>Level {playerData.level}</Text>
          <Text style={styles.playerSubtitle}>Adventurer</Text>
        </View>
      </View>

      <View style={styles.xpSection}>
        <View style={styles.xpHeader}>
          <Text style={styles.xpLabel}>Experience Points</Text>
          <Text style={styles.xpText}>
            {playerData.xpProgress} / {playerData.xpRequiredForNextLevel} XP
          </Text>
        </View>
        <XPProgressBar currentXP={playerData.xpProgress} maxXP={playerData.xpRequiredForNextLevel} />
      </View>

      <View style={styles.statsGrid}>
        <View style={styles.statItem}>
          <View style={styles.statIcon}>
            <Zap size={24} color={colors.primary} />
          </View>
          <Text style={styles.statValue}>{playerData.totalXP.toLocaleString()}</Text>
          <Text style={styles.statLabel}>Total XP</Text>
        </View>

        <View style={styles.statItem}>
          <View style={styles.statIcon}>
            <Flame size={24} color={colors.fire} />
          </View>
          <Text style={styles.statValue}>{playerData.currentActiveStreaks}</Text>
          <Text style={styles.statLabel}>Active Streaks</Text>
        </View>

        <View style={styles.statItem}>
          <View style={styles.statIcon}>
            <Target size={24} color={colors.success} />
          </View>
          <Text style={styles.statValue}>{playerData.totalCompletions}</Text>
          <Text style={styles.statLabel}>Completed</Text>
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.lg,
    marginBottom: spacing.md,
  },
  loadingContainer: {
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: spacing.xl,
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: fontSize.sm,
    marginTop: spacing.sm,
  },
  errorContainer: {
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: spacing.xl,
  },
  errorText: {
    color: colors.danger,
    fontSize: fontSize.sm,
    textAlign: "center",
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: spacing.lg,
  },
  avatarContainer: {
    position: "relative",
    marginRight: spacing.md,
  },
  avatar: {
    width: 64,
    height: 64,
    backgroundColor: `${colors.primary}33`, // 20% opacity
    borderRadius: 32,
    alignItems: "center",
    justifyContent: "center",
  },
  levelBadge: {
    position: "absolute",
    bottom: -4,
    right: -4,
    backgroundColor: colors.gold,
    borderRadius: borderRadius.md,
    minWidth: 24,
    height: 24,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: spacing.xs,
  },
  levelText: {
    fontSize: fontSize.xs,
    fontWeight: "bold",
    color: colors.background,
  },
  playerInfo: {
    flex: 1,
  },
  levelTitle: {
    fontSize: fontSize.xl,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  playerSubtitle: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
  },
  xpSection: {
    marginBottom: spacing.lg,
  },
  xpHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.sm,
  },
  xpLabel: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
  },
  xpText: {
    fontSize: fontSize.sm,
    fontWeight: "600",
    color: colors.textPrimary,
  },
  statsGrid: {
    flexDirection: "row",
    justifyContent: "space-around",
  },
  statItem: {
    alignItems: "center",
    flex: 1,
  },
  statIcon: {
    marginBottom: spacing.sm,
  },
  statValue: {
    fontSize: fontSize.lg,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  statLabel: {
    fontSize: fontSize.xs,
    color: colors.textSecondary,
    textAlign: "center",
    marginTop: spacing.xs,
  },
});
