import React, { useState, useEffect, useCallback } from "react";
import { View, Text, StyleSheet, TouchableOpacity, FlatList, RefreshControl, ActivityIndicator } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Plus, Filter, Archive, RefreshCw } from "lucide-react-native";
import HabitCard from "../components/HabitCard";
import CreateHabitModal from "../components/CreateHabitModal";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { useAuth } from "../context/AuthContext";
import { useHabits } from "../context/HabitsContext";
import { Habit, GameReward, ApiError } from "../types/types";
import api from "../api/api";

export default function HabitsScreen() {
  const { user } = useAuth();
  const { onHabitCreated, onHabitCompleted } = useHabits();
  const [showInactive, setShowInactive] = useState(false);
  const [activeHabits, setActiveHabits] = useState<Habit[]>([]);
  const [inactiveHabits, setInactiveHabits] = useState<Habit[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const displayHabits = showInactive ? inactiveHabits : activeHabits;

  const fetchHabits = useCallback(
    async (showLoading = true) => {
      if (!user) {
        setIsLoading(false);
        setActiveHabits([]);
        setInactiveHabits([]);
        return;
      }

      try {
        if (showLoading) {
          setIsLoading(true);
        }
        setError(null);

        console.log("🚀 Fetching habits...");

        const activeResponse = await api.get("/habits", {
          params: { includeInactive: false },
        });
        const activeHabitsData: Habit[] = activeResponse.data;

        const inactiveResponse = await api.get("/habits/deleted");
        const inactiveHabitsData: Habit[] = inactiveResponse.data;

        console.log(`✅ Loaded ${activeHabitsData.length} active and ${inactiveHabitsData.length} inactive habits`);

        setActiveHabits(activeHabitsData);
        setInactiveHabits(inactiveHabitsData);
      } catch (err) {
        console.error("❌ Error fetching habits:", err);
        const apiError = err as ApiError;

        if (!user) {
          return;
        }

        setError(apiError.message || "Failed to load habits");
      } finally {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    },
    [user]
  );

  const onRefresh = useCallback(async () => {
    setIsRefreshing(true);
    await fetchHabits(false);
  }, [fetchHabits]);

  const handleAddHabit = () => {
    setShowCreateModal(true);
  };

  const handleHabitCreated = (newHabit: Habit) => {
    console.log("✅ New habit created, adding to active habits:", newHabit.title);

    setActiveHabits((prevHabits) => [newHabit, ...prevHabits]);

    if (showInactive) {
      setShowInactive(false);
    }

    onHabitCreated(newHabit);
  };

  const handleHabitComplete = useCallback(
    (habitId: number, reward: GameReward) => {
      setActiveHabits((prevHabits) =>
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

      onHabitCompleted(habitId, reward);
    },
    [onHabitCompleted]
  );

  const handleHabitUpdate = useCallback((updatedHabit: Habit) => {
    if (updatedHabit.isActive) {
      setActiveHabits((prevHabits) => prevHabits.map((habit) => (habit.id === updatedHabit.id ? updatedHabit : habit)));
    } else {
      setInactiveHabits((prevHabits) =>
        prevHabits.map((habit) => (habit.id === updatedHabit.id ? updatedHabit : habit))
      );
    }
  }, []);

  const handleTabSwitch = (inactive: boolean) => {
    setShowInactive(inactive);
  };

  useEffect(() => {
    fetchHabits();
  }, [fetchHabits]);

  const renderHabit = ({ item }: { item: Habit }) => (
    <HabitCard key={item.id} habit={item} onComplete={handleHabitComplete} onUpdate={handleHabitUpdate} />
  );

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      {error ? (
        <>
          <Text style={styles.emptyTitle}>Failed to load habits</Text>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => fetchHabits()}>
            <RefreshCw size={16} color={colors.textPrimary} />
            <Text style={styles.retryText}>Retry</Text>
          </TouchableOpacity>
        </>
      ) : (
        <>
          <Text style={styles.emptyTitle}>{showInactive ? "No inactive habits" : "No active habits yet"}</Text>
          <Text style={styles.emptySubtext}>
            {showInactive
              ? "All your habits are active and ready to go!"
              : "Create your first habit to start your journey!"}
          </Text>
          {!showInactive && (
            <TouchableOpacity style={styles.createButton} onPress={handleAddHabit}>
              <Plus size={18} color={colors.textPrimary} />
              <Text style={styles.createButtonText}>Create Your First Habit</Text>
            </TouchableOpacity>
          )}
        </>
      )}
    </View>
  );

  const renderLoadingState = () => (
    <View style={styles.loadingContainer}>
      <ActivityIndicator size="large" color={colors.primary} />
      <Text style={styles.loadingText}>Loading habits...</Text>
    </View>
  );

  if (isLoading && activeHabits.length === 0 && inactiveHabits.length === 0) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <View style={styles.titleRow}>
            <Text style={styles.title}>My Habits</Text>
            <TouchableOpacity style={styles.addButton} onPress={handleAddHabit}>
              <Plus size={18} color={colors.textPrimary} />
              <Text style={styles.addButtonText}>Add</Text>
            </TouchableOpacity>
          </View>
        </View>
        {renderLoadingState()}
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View style={styles.titleRow}>
          <Text style={styles.title}>My Habits</Text>
          <TouchableOpacity style={styles.addButton} onPress={handleAddHabit}>
            <Plus size={18} color={colors.textPrimary} />
            <Text style={styles.addButtonText}>Add</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.filterTabs}>
          <TouchableOpacity
            style={[styles.filterTab, !showInactive && styles.filterTabActive]}
            onPress={() => handleTabSwitch(false)}
          >
            <Filter size={14} color={!showInactive ? colors.textPrimary : colors.textSecondary} />
            <Text style={[styles.filterTabText, !showInactive && styles.filterTabTextActive]}>
              Active ({activeHabits.length})
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.filterTab, showInactive && styles.filterTabActive]}
            onPress={() => handleTabSwitch(true)}
          >
            <Archive size={14} color={showInactive ? colors.textPrimary : colors.textSecondary} />
            <Text style={[styles.filterTabText, showInactive && styles.filterTabTextActive]}>
              Inactive ({inactiveHabits.length})
            </Text>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.content}>
        <View style={styles.habitCard}>
          <View style={styles.cardHeader}>
            <Text style={styles.cardTitle}>{showInactive ? "Inactive Habits" : "Active Habits"}</Text>
          </View>

          <View style={styles.cardContent}>
            {displayHabits.length > 0 ? (
              <FlatList
                data={displayHabits}
                keyExtractor={(item) => item.id.toString()}
                renderItem={renderHabit}
                showsVerticalScrollIndicator={false}
                refreshControl={
                  <RefreshControl
                    refreshing={isRefreshing}
                    onRefresh={onRefresh}
                    tintColor={colors.primary}
                    colors={[colors.primary]}
                  />
                }
                contentContainerStyle={displayHabits.length === 0 ? styles.emptyListContainer : undefined}
                ItemSeparatorComponent={() => <View style={styles.separator} />}
              />
            ) : (
              renderEmptyState()
            )}
          </View>
        </View>
      </View>

      <CreateHabitModal
        visible={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSuccess={handleHabitCreated}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  header: {
    backgroundColor: colors.cardBackground,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    padding: spacing.md,
  },
  titleRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.sm,
  },
  title: {
    fontSize: fontSize.xxl,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  addButton: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
  },
  addButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.sm,
    fontWeight: "600",
    marginLeft: spacing.xs,
  },
  filterTabs: {
    flexDirection: "row",
    gap: spacing.sm,
  },
  filterTab: {
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: borderRadius.md,
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
  },
  filterTabActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  filterTabText: {
    fontSize: fontSize.xs,
    color: colors.textSecondary,
    marginLeft: spacing.xs,
    fontWeight: "500",
  },
  filterTabTextActive: {
    color: colors.textPrimary,
    fontWeight: "600",
  },
  content: {
    flex: 1,
    padding: spacing.md,
  },
  habitCard: {
    flex: 1,
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: "hidden",
  },
  cardHeader: {
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  cardTitle: {
    fontSize: fontSize.lg,
    fontWeight: "600",
    color: colors.textPrimary,
  },
  cardContent: {
    flex: 1,
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
  emptyTitle: {
    fontSize: fontSize.lg,
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
    marginBottom: spacing.lg,
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
  createButton: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: colors.primary,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    borderRadius: borderRadius.md,
  },
  createButtonText: {
    color: colors.textPrimary,
    fontSize: fontSize.sm,
    fontWeight: "600",
    marginLeft: spacing.xs,
  },
  emptyListContainer: {
    flex: 1,
  },
  separator: {
    height: spacing.sm,
  },
});