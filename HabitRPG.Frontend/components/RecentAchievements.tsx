import React, { useState, useEffect } from "react";
import { View, Text, StyleSheet, TouchableOpacity } from "react-native";
import { Trophy, Award, Star, Zap } from "lucide-react-native";
import { colors, spacing, fontSize, borderRadius } from "../constants/theme";
import { useAuth } from "../context/AuthContext";
import api from "../api/api";

interface Achievement {
  id: string;
  title: string;
  description: string;
  icon: "trophy" | "award" | "star" | "zap";
  unlocked: boolean;
  unlockedAt?: string;
}

export default function RecentAchievements() {
  const { user } = useAuth();
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchAchievements();
  }, [user]);

  const fetchAchievements = async () => {
    if (!user) {
      setIsLoading(false);
      return;
    }

    try {
      // In the future, this would come from an achievements API endpoint
      const mockAchievements: Achievement[] = [
        {
          id: "first-habit",
          title: "Getting Started",
          description: "Create your first habit",
          icon: "star",
          unlocked: true,
          unlockedAt: new Date().toISOString(),
        },
        {
          id: "level-5",
          title: "Rising Star",
          description: "Reach level 5",
          icon: "trophy",
          unlocked: user.level >= 5,
          unlockedAt: user.level >= 5 ? new Date().toISOString() : undefined,
        },
        {
          id: "streak-7",
          title: "Week Warrior",
          description: "Maintain a 7-day streak",
          icon: "award",
          unlocked: false, // This would come from actual streak data
        },
        {
          id: "complete-100",
          title: "Centurion",
          description: "Complete 100 habits",
          icon: "zap",
          unlocked: false, // This would come from completion count
        },
      ];

      const sorted = mockAchievements.sort((a, b) => {
        if (a.unlocked && !b.unlocked) return -1;
        if (!a.unlocked && b.unlocked) return 1;
        if (a.unlockedAt && b.unlockedAt) {
          return new Date(b.unlockedAt).getTime() - new Date(a.unlockedAt).getTime();
        }
        return 0;
      });

      setAchievements(sorted.slice(0, 3));
      setIsLoading(false);
    } catch (error) {
      console.error("Error fetching achievements:", error);
      setIsLoading(false);
    }
  };

  const getIcon = (iconType: string) => {
    const iconProps = { size: 24, color: colors.primary };
    switch (iconType) {
      case "trophy":
        return <Trophy {...iconProps} />;
      case "award":
        return <Award {...iconProps} />;
      case "star":
        return <Star {...iconProps} />;
      case "zap":
        return <Zap {...iconProps} />;
      default:
        return <Star {...iconProps} />;
    }
  };

  if (isLoading) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Recent Achievements</Text>
        </View>
        <View style={styles.content}>
          <Text style={styles.emptyText}>Loading achievements...</Text>
        </View>
      </View>
    );
  }

  if (achievements.length === 0) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>Recent Achievements</Text>
        </View>
        <View style={styles.content}>
          <Text style={styles.emptyText}>No achievements yet</Text>
          <Text style={styles.emptySubtext}>Complete habits to unlock achievements!</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>Recent Achievements</Text>
      </View>
      <View style={styles.content}>
        {achievements.map((achievement) => (
          <View
            key={achievement.id}
            style={[styles.achievementItem, !achievement.unlocked && styles.achievementLocked]}
          >
            <View style={[styles.iconContainer, !achievement.unlocked && styles.iconContainerLocked]}>
              {getIcon(achievement.icon)}
            </View>
            <View style={styles.achievementInfo}>
              <Text style={[styles.achievementTitle, !achievement.unlocked && styles.achievementTitleLocked]}>
                {achievement.title}
              </Text>
              <Text
                style={[styles.achievementDescription, !achievement.unlocked && styles.achievementDescriptionLocked]}
              >
                {achievement.description}
              </Text>
              {achievement.unlocked && achievement.unlockedAt && (
                <Text style={styles.unlockedDate}>
                  Unlocked {new Date(achievement.unlockedAt).toLocaleDateString()}
                </Text>
              )}
            </View>
          </View>
        ))}
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
  achievementItem: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: spacing.md,
    padding: spacing.sm,
    borderRadius: borderRadius.md,
    backgroundColor: `${colors.primary}10`,
  },
  achievementLocked: {
    backgroundColor: colors.surfaceBackground,
    opacity: 0.6,
  },
  iconContainer: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: `${colors.primary}20`,
    justifyContent: "center",
    alignItems: "center",
    marginRight: spacing.md,
  },
  iconContainerLocked: {
    backgroundColor: colors.border,
  },
  achievementInfo: {
    flex: 1,
  },
  achievementTitle: {
    fontSize: fontSize.md,
    fontWeight: "600",
    color: colors.textPrimary,
    marginBottom: spacing.xs / 2,
  },
  achievementTitleLocked: {
    color: colors.textMuted,
  },
  achievementDescription: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
  },
  achievementDescriptionLocked: {
    color: colors.textMuted,
  },
  unlockedDate: {
    fontSize: fontSize.xs,
    color: colors.success,
    fontWeight: "500",
  },
  emptyText: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
    textAlign: "center",
    marginBottom: spacing.xs,
  },
  emptySubtext: {
    fontSize: fontSize.sm,
    color: colors.textMuted,
    textAlign: "center",
  },
});