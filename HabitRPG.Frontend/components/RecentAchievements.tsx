import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { colors, spacing, borderRadius, fontSize } from "../constants/theme";

interface XPProgressBarProps {
  currentXP: number;
  maxXP: number;
}

export default function XPProgressBar({ currentXP, maxXP }: XPProgressBarProps) {
  const progress = maxXP > 0 ? Math.min((currentXP / maxXP) * 100, 100) : 0;

  return (
    <View style={styles.container}>
      <View style={styles.progressBackground}>
        <View style={[styles.progressFill, { width: `${progress}%` }]} />
      </View>
      <Text style={styles.xpText}>
        {currentXP} / {maxXP} XP
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    marginVertical: spacing.sm,
  },
  progressBackground: {
    height: 12,
    backgroundColor: colors.xpBarBackground,
    borderRadius: borderRadius.sm,
    overflow: "hidden",
  },
  progressFill: {
    height: "100%",
    backgroundColor: colors.xpBar,
    borderRadius: borderRadius.sm,
  },
  xpText: {
    fontSize: fontSize.sm,
    color: colors.textSecondary,
    textAlign: "center",
    marginTop: spacing.xs,
  },
});
