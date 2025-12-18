import React, { ReactNode } from "react";
import { View, Text, StyleSheet, TouchableOpacity } from "react-native";
import { colors } from "../constants/theme";
import ErrorBoundary from "./ErrorBoundary";

interface Props {
  children: ReactNode;
  screenName?: string;
}

const ScreenErrorFallback: React.FC<{ onReset: () => void; screenName?: string }> = ({ onReset, screenName }) => {
  return (
    <View style={styles.container}>
      <View style={styles.content}>
        <Text style={styles.emoji}>⚠️</Text>
        <Text style={styles.title}>Something went wrong</Text>
        <Text style={styles.message}>
          {screenName ? `An error occurred on the ${screenName} screen.` : "An error occurred on this screen."}
        </Text>
        <TouchableOpacity style={styles.button} onPress={onReset}>
          <Text style={styles.buttonText}>Reload Screen</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
};

const ScreenErrorBoundary: React.FC<Props> = ({ children, screenName }) => {
  return (
    <ErrorBoundary fallback={<ScreenErrorFallback onReset={() => {}} screenName={screenName} />}>
      {children}
    </ErrorBoundary>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
    justifyContent: "center",
    alignItems: "center",
    padding: 20,
  },
  content: {
    alignItems: "center",
    maxWidth: 300,
  },
  emoji: {
    fontSize: 48,
    marginBottom: 16,
  },
  title: {
    fontSize: 20,
    fontWeight: "bold",
    color: colors.textPrimary,
    marginBottom: 8,
    textAlign: "center",
  },
  message: {
    fontSize: 14,
    color: colors.textSecondary,
    textAlign: "center",
    marginBottom: 24,
    lineHeight: 20,
  },
  button: {
    backgroundColor: colors.primary,
    paddingHorizontal: 24,
    paddingVertical: 10,
    borderRadius: 8,
  },
  buttonText: {
    color: "#ffffff",
    fontSize: 14,
    fontWeight: "600",
  },
});

export default ScreenErrorBoundary;