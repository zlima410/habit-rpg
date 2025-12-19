import React, { useCallback, useState } from "react";
import { ScrollView, StyleSheet, Alert, ActivityIndicator } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useNavigation } from "@react-navigation/native";
import { BottomTabNavigationProp } from "@react-navigation/bottom-tabs";
import PlayerStats from "../components/PlayerStats";
import QuickActions from "../components/QuickActions";
import TodayHabits from "../components/TodayHabits";
import RecentAchievements from "../components/RecentAchievements";
import { colors, spacing } from "../constants/theme";
import { Habit, GameReward, ApiError } from "../types/types";
import { useHabits } from "../context/HabitsContext";
import { useAuth } from "../context/AuthContext";
import api from "../api/api";

type RootTabParamList = {
  Dashboard: undefined;
  Habits: undefined;
  Stats: undefined;
  Profile: undefined;
};

type NavigationProp = BottomTabNavigationProp<RootTabParamList>;

export default function DashboardScreen() {
  const navigation = useNavigation<NavigationProp>();
  const { user } = useAuth();
  const { refreshTrigger, onHabitCreated, onHabitCompleted, triggerRefresh } = useHabits();
  const [isCompletingAll, setIsCompletingAll] = useState(false);

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

  const handleCompleteAll = useCallback(async () => {
    if (!user) {
      Alert.alert("Error", "You must be logged in to complete habits");
      return;
    }

    Alert.alert("Complete All Habits", "This will mark all remaining habits for today as complete. Continue?", [
      { text: "Cancel", style: "cancel" },
      {
        text: "Complete All",
        style: "default",
        onPress: async () => {
          try {
            setIsCompletingAll(true);
            console.log("🚀 Completing all habits for today...");

            const response = await api.get("/habits", {
              params: { includeInactive: false },
            });

            const habits: Habit[] = response.data;
            const incompleteHabits = habits.filter((habit) => habit.canCompleteToday && habit.isActive);

            if (incompleteHabits.length === 0) {
              Alert.alert("All Done!", "You've already completed all your habits for today! 🎉");
              setIsCompletingAll(false);
              return;
            }

            let completedCount = 0;
            let totalXpGained = 0;
            let leveledUp = false;
            let newLevel = user.level;

            for (const habit of incompleteHabits) {
              try {
                const completeResponse = await api.post(`/habits/${habit.id}/complete`);
                const reward: GameReward = completeResponse.data;

                if (reward.success) {
                  completedCount++;
                  totalXpGained += reward.xpGained;
                  if (reward.leveledUp) {
                    leveledUp = true;
                    newLevel = reward.newLevel;
                  }

                  if (onHabitCompleted) {
                    onHabitCompleted(habit.id, reward);
                  }
                }
              } catch (err) {
                console.error(`❌ Error completing habit ${habit.id}:`, err);
              }
            }

            setIsCompletingAll(false);
            triggerRefresh();

            if (leveledUp) {
              Alert.alert(
                "🎉 Level Up!",
                `Amazing! You completed ${completedCount} habits and gained ${totalXpGained} XP! You've reached level ${newLevel}!`,
                [{ text: "Awesome!" }]
              );
            } else {
              Alert.alert("Great Job!", `You completed ${completedCount} habits and gained ${totalXpGained} XP!`, [
                { text: "Nice!" },
              ]);
            }
          } catch (err) {
            setIsCompletingAll(false);
            console.error("❌ Error completing all habits:", err);
            const error = err as ApiError;
            Alert.alert("Error", error.message || "Failed to complete all habits. Please try again.");
          }
        },
      },
    ]);
  }, [user, onHabitCompleted, triggerRefresh]);

  const handleViewStats = useCallback(() => {
    navigation.navigate("Stats");
  }, [navigation]);

  const handleSettings = useCallback(() => {
    navigation.navigate("Profile");
  }, [navigation]);

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

        {isCompletingAll && (
          <ActivityIndicator size="large" color={colors.primary} style={styles.completingIndicator} />
        )}

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
  completingIndicator: {
    marginVertical: spacing.md,
    alignSelf: "center",
  },
});