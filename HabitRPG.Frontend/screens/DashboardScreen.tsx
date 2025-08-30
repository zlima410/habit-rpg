import React, { useCallback } from "react";
import { ScrollView, StyleSheet, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import PlayerStats from "../components/PlayerStats";
import QuickActions from "../components/QuickActions";
import TodayHabits from "../components/TodayHabits";
import RecentAchievements from "../components/RecentAchievements";
import { colors, spacing } from "../constants/theme";
import { Habit, GameReward } from "../types/types";
import { useHabits } from "../context/HabitsContext";

export default function DashboardScreen() {
  const { refreshTrigger, onHabitCreated, onHabitCompleted } = useHabits();

  const handleHabitCreated = useCallback(
    (newHabit: Habit) => {
      console.log("✅ New habit created on dashboard:", newHabit.title);

      onHabitCreated(newHabit);

      Alert.alert(
        "Habit Created!",
        `"${newHabit.title}" has been added to your habits. You can complete it from the Today's Quests section.`,
        [{ text: "Got it!" }]
      );
    },
    [onHabitCreated]
  );

  const handleCompleteAll = useCallback(() => {
    Alert.alert(
      "Complete All Habits",
      "This feature will mark all remaining habits for today as complete. This is coming soon!",
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Complete All",
          style: "default",
          onPress: () => {
            // TODO: Implement complete all functionality
            console.log("Would complete all habits...");
          },
        },
      ]
    );
  }, []);

  const handleViewStats = useCallback(() => {
    // TODO: Navigate to stats tab/screen
    Alert.alert("View Stats", "Navigating to statistics...");
  }, []);

  const handleSettings = useCallback(() => {
    // TODO: Navigate to settings screen
    Alert.alert("Settings", "Settings feature coming soon!");
  }, []);

  const handleHabitComplete = useCallback(
    (habitId: number, reward: GameReward) => {
      console.log(`🎉 Habit ${habitId} completed from dashboard! +${reward.xpGained} XP`);

      onHabitCompleted(habitId, reward);

      if (reward.leveledUp) {
        Alert.alert("🎉 Level Up!", `Congratulations! You've reached level ${reward.newLevel}!`, [
          { text: "Awesome!" },
        ]);
      }
    },
    [onHabitCompleted]
  );

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView style={styles.scrollView} showsVerticalScrollIndicator={false}>
        <PlayerStats refreshTrigger={refreshTrigger} />

        <QuickActions
          onAddHabit={handleHabitCreated}
          onCompleteAll={handleCompleteAll}
          onViewStats={handleViewStats}
          onSettings={handleSettings}
        />

        <TodayHabits refreshTrigger={refreshTrigger} onHabitComplete={handleHabitComplete} />

        <RecentAchievements />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  scrollView: {
    flex: 1,
    padding: spacing.md,
  },
});