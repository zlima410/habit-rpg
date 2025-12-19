import React, { useState, useEffect, useCallback } from "react";
import { View, Text, StyleSheet, ScrollView, RefreshControl, ActivityIndicator } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { BarChart3, TrendingUp, Calendar, Target } from "lucide-react-native";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { useAuth } from "../context/AuthContext";
import { UserStats, ApiError } from "../types/types";
import api from "../api/api";
import { StatsCardSkeleton, StatBoxSkeleton } from "../components/skeletons";

export default function StatsScreen() {
  const { user } = useAuth();
  const [statsData, setStatsData] = useState<UserStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStats = useCallback(async () => {
    if (!user) {
      setIsLoading(false);
      return;
    }

    try {
      setError(null);
      const response = await api.get("/user/stats?days=30");
      const stats: UserStats = response.data;
      setStatsData(stats);
    } catch (err) {
      console.error("❌ Error fetching stats:", err);
      const apiError = err as ApiError;
      setError(apiError.message || "Failed to load statistics");
    } finally {
      setIsLoading(false);
      setRefreshing(false);
    }
  }, [user]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    fetchStats();
  }, [fetchStats]);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Statistics</Text>
          <Text style={styles.subtitle}>Track your progress</Text>
        </View>
        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <StatsCardSkeleton />
          <StatsCardSkeleton />
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <TrendingUp size={20} color={colors.textPrimary} />
              <Text style={styles.cardTitle}>Experience Points</Text>
            </View>
            <View style={styles.cardContent}>
              <View style={styles.statsGrid}>
                <StatBoxSkeleton />
                <StatBoxSkeleton />
              </View>
            </View>
          </View>
          <View style={styles.card}>
            <View style={styles.cardHeader}>
              <Calendar size={20} color={colors.textPrimary} />
              <Text style={styles.cardTitle}>Streaks</Text>
            </View>
            <View style={styles.cardContent}>
              <View style={styles.statsGrid}>
                <StatBoxSkeleton />
                <StatBoxSkeleton />
              </View>
            </View>
          </View>
          <View style={styles.bottomSpacing} />
        </ScrollView>
      </SafeAreaView>
    );
  }

  if (error && !statsData) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
        </View>
      </SafeAreaView>
    );
  }

  const weeklyCompletion = statsData?.completionRate || 0;
  const monthlyCompletion = statsData?.completionRate || 0;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Statistics</Text>
        <Text style={styles.subtitle}>Track your progress</Text>
      </View>

      <ScrollView
        style={styles.content}
        showsVerticalScrollIndicator={false}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={colors.primary}
            colors={[colors.primary]}
          />
        }
      >
        {/* Completion Rates */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <BarChart3 size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Completion Rates</Text>
          </View>
          <View style={styles.cardContent}>
            <View style={styles.progressSection}>
              <View style={styles.progressHeader}>
                <Text style={styles.progressLabel}>This Week</Text>
                <Text style={styles.progressValue}>{weeklyCompletion.toFixed(1)}%</Text>
              </View>
              <View style={styles.progressBarContainer}>
                <View
                  style={[
                    styles.progressBarFill,
                    { width: `${Math.min(weeklyCompletion, 100)}%`, backgroundColor: colors.primary },
                  ]}
                />
              </View>
            </View>
            <View style={styles.progressSection}>
              <View style={styles.progressHeader}>
                <Text style={styles.progressLabel}>This Month</Text>
                <Text style={styles.progressValue}>{monthlyCompletion.toFixed(1)}%</Text>
              </View>
              <View style={styles.progressBarContainer}>
                <View
                  style={[
                    styles.progressBarFill,
                    { width: `${Math.min(monthlyCompletion, 100)}%`, backgroundColor: colors.warning },
                  ]}
                />
              </View>
            </View>
          </View>
        </View>

        {/* XP Earned */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <TrendingUp size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Experience Points</Text>
          </View>
          <View style={styles.cardContent}>
            <View style={styles.statsGrid}>
              <View style={[styles.statBox, { backgroundColor: `${colors.primary}15` }]}>
                <Text style={[styles.statValue, { color: colors.primary }]}>{statsData?.totalCompletions || 0}</Text>
                <Text style={styles.statLabel}>Total Completions</Text>
              </View>
              <View style={[styles.statBox, { backgroundColor: `${colors.warning}15` }]}>
                <Text style={[styles.statValue, { color: colors.warning }]}>
                  {statsData?.averageCompletionsPerDay.toFixed(1) || 0}
                </Text>
                <Text style={styles.statLabel}>Avg Per Day</Text>
              </View>
            </View>
          </View>
        </View>

        {/* Streaks */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Calendar size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Streaks</Text>
          </View>
          <View style={styles.cardContent}>
            <View style={styles.statsGrid}>
              <View style={[styles.statBox, { backgroundColor: `${colors.fire}15` }]}>
                <Text style={[styles.statValue, { color: colors.fire }]}>{statsData?.currentStreak || 0}</Text>
                <Text style={styles.statLabel}>Current Streak</Text>
              </View>
              <View style={[styles.statBox, { backgroundColor: `${colors.gold}15` }]}>
                <Text style={[styles.statValue, { color: colors.gold }]}>{statsData?.longestStreakInPeriod || 0}</Text>
                <Text style={styles.statLabel}>Best Streak</Text>
              </View>
            </View>
          </View>
        </View>

        {/* Habits Completed */}
        <View style={styles.card}>
          <View style={styles.cardHeader}>
            <Target size={20} color={colors.textPrimary} />
            <Text style={styles.cardTitle}>Habits Completed</Text>
          </View>
          <View style={styles.cardContent}>
            <View style={styles.statsGrid}>
              <View style={[styles.statBox, { backgroundColor: `${colors.success}15` }]}>
                <Text style={[styles.statValue, { color: colors.success }]}>{statsData?.totalCompletions || 0}</Text>
                <Text style={styles.statLabel}>Total</Text>
              </View>
              <View style={[styles.statBox, { backgroundColor: `${colors.primary}15` }]}>
                <Text style={[styles.statValue, { color: colors.primary }]}>
                  {statsData?.averageCompletionsPerDay.toFixed(1) || 0}
                </Text>
                <Text style={styles.statLabel}>Per Day</Text>
              </View>
            </View>
          </View>
        </View>

        <View style={styles.bottomSpacing} />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: fontSize.md,
    marginTop: spacing.sm,
  },
  errorContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: spacing.lg,
  },
  errorText: {
    color: colors.danger,
    fontSize: fontSize.md,
    textAlign: "center",
  },
  header: {
    backgroundColor: colors.cardBackground,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    padding: spacing.md,
  },
  title: {
    fontSize: fontSize.xxl,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  subtitle: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
    marginTop: spacing.xs,
  },
  content: {
    flex: 1,
    padding: spacing.md,
  },
  card: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: "hidden",
  },
  cardHeader: {
    flexDirection: "row",
    alignItems: "center",
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  cardTitle: {
    fontSize: fontSize.lg,
    fontWeight: "600",
    color: colors.textPrimary,
    marginLeft: spacing.sm,
  },
  cardContent: {
    padding: spacing.md,
  },
  progressSection: {
    marginBottom: spacing.lg,
  },
  progressHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.sm,
  },
  progressLabel: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
  },
  progressValue: {
    fontSize: fontSize.md,
    fontWeight: "600",
    color: colors.textPrimary,
  },
  progressBarContainer: {
    width: "100%",
    height: 8,
    backgroundColor: `${colors.textSecondary}30`,
    borderRadius: borderRadius.sm,
    overflow: "hidden",
  },
  progressBarFill: {
    height: "100%",
    borderRadius: borderRadius.sm,
  },
  statsGrid: {
    flexDirection: "row",
    justifyContent: "space-around",
    gap: spacing.md,
  },
  statBox: {
    flex: 1,
    padding: spacing.lg,
    borderRadius: borderRadius.md,
    alignItems: "center",
  },
  statValue: {
    fontSize: fontSize.xxl,
    fontWeight: "bold",
    marginBottom: spacing.xs,
  },
  statLabel: {
    fontSize: fontSize.xs,
    color: colors.textSecondary,
    textAlign: "center",
  },
  bottomSpacing: {
    height: spacing.xl,
  },
});
