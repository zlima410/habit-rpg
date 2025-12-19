import React, { useState, useEffect, useCallback } from "react";
import { View, Text, StyleSheet, FlatList, RefreshControl, ActivityIndicator, TouchableOpacity } from "react-native";
import { RefreshCw } from "lucide-react-native";
import HabitCard from "../components/HabitCard";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { useAuth } from "../context/AuthContext";
import { Habit, GameReward, ApiError } from "../types/types";
import api from "../api/api";
import { HabitListSkeleton } from "./skeletons";

interface TodayHabitsProps {
  refreshTrigger?: number;
  onHabitComplete?: (habitId: number, reward: GameReward) => void;
  onRefresh?: (refreshFunction: () => Promise<void>) => void;
  isRefreshing?: boolean;
}

export default function TodayHabits({
  refreshTrigger,
  onHabitComplete,
  onRefresh,
  isRefreshing: parentIsRefreshing,
}: TodayHabitsProps) {
  const { user } = useAuth();
  const [habits, setHabits] = useState<Habit[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchTodayHabits = useCallback(
    async (showLoading = true) => {
      if (!user) {
        setIsLoading(false);
        setHabits([]);
        return;
      }

      try {
        if (showLoading) {
          setIsLoading(true);
        }
        setError(null);

        console.log("🚀 Fetching today's habits...");

        const response = await api.get("/habits", {
          params: { includeInactive: false },
        });

        const allHabits: Habit[] = response.data;

        const todayHabits = allHabits.filter((habit) => {
          const today = new Date().toDateString();
          const lastCompleted = habit.lastCompletedAt ? new Date(habit.lastCompletedAt).toDateString() : null;

          return habit.canCompleteToday || lastCompleted === today;
        });

        console.log(`✅ Found ${todayHabits.length} habits for today`);
        setHabits(todayHabits);
      } catch (err) {
        console.error("❌ Error fetching today's habits:", err);
        const apiError = err as ApiError;

        if (!user) {
          return;
        }

        setError(apiError.message || "Failed to load today's habits");
      } finally {
        setIsLoading(false);
      }
    },
    [user]
  );

  const refreshHabits = useCallback(async () => {
    await fetchTodayHabits(false);
  }, [fetchTodayHabits]);

  useEffect(() => {
    if (onRefresh) {
      onRefresh(refreshHabits);
    }
  }, [refreshHabits, onRefresh]);

  const handleHabitComplete = useCallback(
    (habitId: number, reward: GameReward) => {
      setHabits((prevHabits) =>
        prevHabits.map((habit) =>
          habit.id === habitId
            ? {
                ...habit,
                canCompleteToday: false,
                currentStreak: reward.updatedHabit?.currentStreak || habit.currentStreak,
                bestStreak: reward.updatedHabit?.bestStreak || habit.bestStreak,
                lastCompletedAt: new Date().toISOString(),
              }
            : habit
        )
      );

      if (onHabitComplete) {
        onHabitComplete(habitId, reward);
      }
    },
    [onHabitComplete]
  );

  const handleHabitUpdate = useCallback((updatedHabit: Habit) => {
    setHabits((prevHabits) => prevHabits.map((habit) => (habit.id === updatedHabit.id ? updatedHabit : habit)));
  }, []);

  useEffect(() => {
    fetchTodayHabits();
  }, [fetchTodayHabits, refreshTrigger]);

  const completedCount = habits.filter((habit) => {
    const today = new Date().toDateString();
    const lastCompleted = habit.lastCompletedAt ? new Date(habit.lastCompletedAt).toDateString() : null;
    return lastCompleted === today;
  }).length;

  const totalCount = habits.length;

  const renderHabit = ({ item }: { item: Habit }) => (
    <HabitCard key={item.id} habit={item} onComplete={handleHabitComplete} onUpdate={handleHabitUpdate} />
  );

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      {error ? (
        <>
          <Text style={styles.emptyText}>Failed to load habits</Text>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => fetchTodayHabits()}>
            <RefreshCw size={16} color={colors.textPrimary} />
            <Text style={styles.retryText}>Retry</Text>
          </TouchableOpacity>
        </>
      ) : (
        <>
          <Text style={styles.emptyText}>No habits for today</Text>
          <Text style={styles.emptySubtext}>Create some habits to get started on your journey!</Text>
        </>
      )}
    </View>
  );

  const renderLoadingState = () => (
    <View style={styles.loadingContainer}>
      <ActivityIndicator size="large" color={colors.primary} />
      <Text style={styles.loadingText}>Loading today's habits...</Text>
    </View>
  );

  if (isLoading && habits.length === 0) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Today's Quests</Text>
        </View>
        <View style={styles.content}>
          <HabitListSkeleton count={3} />
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View>
          <Text style={styles.title}>Today's Quests</Text>
          <Text style={styles.progressText}>
            {completedCount}/{totalCount} completed
          </Text>
        </View>
        {parentIsRefreshing && <ActivityIndicator size="small" color={colors.primary} />}
      </View>

      <View style={styles.content}>
        {habits.length > 0 ? (
          <View style={styles.listContent}>
            {habits.map((habit, index) => (
              <View key={habit.id.toString()}>
                {renderHabit({ item: habit })}
                {index < habits.length - 1 && <View style={styles.separator} />}
              </View>
            ))}
          </View>
        ) : (
          renderEmptyState()
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: "hidden",
  },
  header: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  title: {
    fontSize: fontSize.lg,
    fontWeight: "600",
    color: colors.textPrimary,
  },
  progressText: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
    fontWeight: "500",
    marginTop: spacing.xs / 2,
  },
  refreshButton: {
    padding: spacing.sm,
    borderRadius: borderRadius.md,
    backgroundColor: `${colors.primary}10`,
  },
  content: {
    minHeight: 120,
  },
  listContent: {
    padding: spacing.md,
  },
  separator: {
    height: spacing.sm,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingVertical: spacing.xl,
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: fontSize.sm,
    marginTop: spacing.sm,
  },
  emptyState: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    paddingVertical: spacing.xl,
    paddingHorizontal: spacing.lg,
  },
  emptyText: {
    fontSize: fontSize.md,
    fontWeight: "600",
    color: colors.textPrimary,
    textAlign: "center",
    marginBottom: spacing.xs,
  },
  emptySubtext: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
    textAlign: "center",
    lineHeight: fontSize.sm * 1.4,
  },
  errorText: {
    fontSize: fontSize.sm,
    color: colors.danger,
    textAlign: "center",
    marginTop: spacing.xs,
    marginBottom: spacing.md,
  },
  retryButton: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
  },
  retryText: {
    color: colors.textPrimary,
    fontSize: fontSize.sm,
    fontWeight: "600",
    marginLeft: spacing.xs,
  },
});