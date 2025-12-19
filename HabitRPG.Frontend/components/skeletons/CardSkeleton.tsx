import React from "react";
import { View, StyleSheet } from "react-native";
import Skeleton from "./Skeleton";
import { colors, spacing, borderRadius } from "../../constants/theme";

interface CardSkeletonProps {
  showHeader?: boolean;
  showContent?: boolean;
  showFooter?: boolean;
}

const CardSkeleton: React.FC<CardSkeletonProps> = ({ showHeader = true, showContent = true, showFooter = false }) => {
  return (
    <View style={styles.container}>
      {showHeader && (
        <View style={styles.header}>
          <Skeleton width={40} height={40} borderRadius={20} />
          <View style={styles.headerText}>
            <Skeleton width={120} height={16} borderRadius={4} />
            <Skeleton width={80} height={12} borderRadius={4} style={styles.headerSubtext} />
          </View>
        </View>
      )}

      {showContent && (
        <View style={styles.content}>
          <Skeleton width="100%" height={14} borderRadius={4} />
          <Skeleton width="90%" height={14} borderRadius={4} style={styles.contentLine} />
          <Skeleton width="75%" height={14} borderRadius={4} />
        </View>
      )}

      {showFooter && (
        <View style={styles.footer}>
          <Skeleton width={80} height={32} borderRadius={4} />
          <Skeleton width={80} height={32} borderRadius={4} />
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.sm,
  },
  header: {
    flexDirection: "row",
    alignItems: "center",
    marginBottom: spacing.md,
  },
  headerText: {
    marginLeft: spacing.md,
    flex: 1,
  },
  headerSubtext: {
    marginTop: spacing.xs,
  },
  content: {
    marginBottom: spacing.md,
  },
  contentLine: {
    marginTop: spacing.sm,
    marginBottom: spacing.sm,
  },
  footer: {
    flexDirection: "row",
    justifyContent: "flex-end",
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
});

export default CardSkeleton;