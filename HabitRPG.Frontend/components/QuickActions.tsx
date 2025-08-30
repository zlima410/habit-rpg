import React, { useState } from "react";
import { View, Text, StyleSheet, TouchableOpacity, Alert } from "react-native";
import { Plus, CheckCircle, BarChart3, Settings } from "lucide-react-native";
import CreateHabitModal from "../components/CreateHabitModal";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { Habit } from "../types/types";

interface QuickActionsProps {
  onAddHabit?: (newHabit: Habit) => void;
  onCompleteAll?: () => void;
  onViewStats?: () => void;
  onSettings?: () => void;
}

export default function QuickActions({ onAddHabit, onCompleteAll, onViewStats, onSettings }: QuickActionsProps) {
  const [showCreateModal, setShowCreateModal] = useState(false);

  const handleAddHabit = () => {
    setShowCreateModal(true);
  };

  const handleHabitCreated = (newHabit: Habit) => {
    if (onAddHabit) {
      onAddHabit(newHabit);
    }
    console.log("✅ New habit created:", newHabit.title);
  };

  const handleCompleteAll = () => {
    if (onCompleteAll) {
      onCompleteAll();
    } else {
      // show confirmation dialog
      Alert.alert("Complete All Habits", "Mark all remaining habits for today as complete?", [
        { text: "Cancel", style: "cancel" },
        {
          text: "Complete All",
          style: "default",
          onPress: () => {
            // trigger completion logic
            console.log("Completing all habits...");
          },
        },
      ]);
    }
  };

  const handleViewStats = () => {
    if (onViewStats) {
      onViewStats();
    } else {
      // Default behavior - navigate to stats screen
      Alert.alert("View Stats", "Navigating to statistics...");
    }
  };

  const handleSettings = () => {
    if (onSettings) {
      onSettings();
    } else {
      // Default behavior - could navigate to settings screen
      Alert.alert("Settings", "Settings feature coming soon!");
    }
  };

  const actions = [
    {
      id: "add-habit",
      icon: Plus,
      label: "Add Habit",
      backgroundColor: `${colors.primary}15`,
      borderColor: `${colors.primary}30`,
      iconColor: colors.primary,
      onPress: handleAddHabit,
    },
    {
      id: "complete-all",
      icon: CheckCircle,
      label: "Complete All",
      backgroundColor: `${colors.success}15`,
      borderColor: `${colors.success}30`,
      iconColor: colors.success,
      onPress: handleCompleteAll,
    },
    {
      id: "view-stats",
      icon: BarChart3,
      label: "View Stats",
      backgroundColor: `${colors.warning}15`,
      borderColor: `${colors.warning}30`,
      iconColor: colors.warning,
      onPress: handleViewStats,
    },
    {
      id: "settings",
      icon: Settings,
      label: "Settings",
      backgroundColor: `${colors.textSecondary}15`,
      borderColor: `${colors.border}`,
      iconColor: colors.textSecondary,
      onPress: handleSettings,
    },
  ];

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Quick Actions</Text>
      </View>

      <View style={styles.content}>
        <View style={styles.actionsGrid}>
          {actions.map((action) => {
            const IconComponent = action.icon;
            return (
              <TouchableOpacity
                key={action.id}
                style={[
                  styles.actionButton,
                  {
                    backgroundColor: action.backgroundColor,
                    borderColor: action.borderColor,
                  },
                ]}
                onPress={action.onPress}
                activeOpacity={0.7}
              >
                <View style={styles.iconContainer}>
                  <IconComponent size={24} color={action.iconColor} />
                </View>
                <Text style={styles.actionLabel}>{action.label}</Text>
              </TouchableOpacity>
            );
          })}
        </View>
      </View>

      <CreateHabitModal
        visible={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSuccess={handleHabitCreated}
      />
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
    padding: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  title: {
    fontSize: fontSize.lg,
    fontWeight: "600",
    color: colors.textPrimary,
  },
  content: {
    padding: spacing.md,
  },
  actionsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    justifyContent: "space-between",
    gap: spacing.sm,
  },
  actionButton: {
    width: "48%",
    borderWidth: 1,
    borderRadius: borderRadius.md,
    paddingVertical: spacing.lg,
    paddingHorizontal: spacing.md,
    alignItems: "center",
    justifyContent: "center",
    minHeight: 80,
  },
  iconContainer: {
    marginBottom: spacing.sm,
  },
  actionLabel: {
    fontSize: fontSize.sm,
    fontWeight: "500",
    color: colors.textPrimary,
    textAlign: "center",
  },
});