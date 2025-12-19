import React from "react";
import { View, StyleSheet } from "react-native";
import Skeleton from "./Skeleton";
import { spacing } from "../../constants/theme";

interface TextSkeletonProps {
  lines?: number;
  width?: number | string;
  lineHeight?: number;
  spacing?: number;
}

const TextSkeleton: React.FC<TextSkeletonProps> = ({
  lines = 3,
  width = "100%",
  lineHeight = 16,
  spacing: lineSpacing = 8,
}) => {
  return (
    <View style={styles.container}>
      {Array.from({ length: lines }).map((_, index) => (
        <Skeleton
          key={index}
          width={index === lines - 1 ? "80%" : width}
          height={lineHeight}
          borderRadius={4}
          style={index < lines - 1 ? { marginBottom: lineSpacing } : undefined}
        />
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    width: "100%",
  },
});

export default TextSkeleton;