"use client"

import React, { useState, useEffect } from "react"
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  Alert,
  ActivityIndicator,
} from "react-native"
import { SafeAreaView } from "react-native-safe-area-context"
import { Ionicons } from "@expo/vector-icons"
import { colors, spacing, borderRadius, fontSize } from "../constants/theme"
import { useAuth } from "../context/AuthContext"
import { Sword, Shield, Star } from "lucide-react-native";

interface AuthScreenProps {
  onLogin: () => void
}

export default function AuthScreen({ onLogin }: AuthScreenProps) {
  const { login, register, error, isLoading, clearError } = useAuth();
  const [isLogin, setIsLogin] = useState(true)
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [username, setUsername] = useState("")
  
  useEffect(() => {
    clearError();
  }, [isLogin, clearError]);

  const validateForm = () => {
    if (!email.trim()) {
        Alert.alert('Error', 'Email is required');
        return false;
    }

    if (!email.includes('@')) {
        Alert.alert('Error', 'Please enter a valid email');
        return false;
    }

    if (password.length < 6) {
        Alert.alert('Error', 'Password must be at least 6 characters');
        return false;
    }

    if (!isLogin && !username.trim()) {
        Alert.alert('Error', 'Username is required');
        return false;
    }

    return true;
  };

  const handleSubmit = async () => {
    if (!validateForm()) return;

    try {
        if (isLogin)
            await login({ email: email.trim(), password });
        else 
            await register({
                username: username.trim(),
                email: email.trim(),
                password
            });

        onLogin();
    } catch (error) {
        console.log("Auth error handled by context");
    }
  };

  const switchMode = () => {
    setIsLogin(!isLogin);
    setEmail('');
    setPassword('');
    setUsername('');
  };

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
        <View style={styles.header}>
          <View style={styles.logo}>
            <View style={{ position: "relative" }}>
              <Shield size={64} color={colors.primary} />
              <Sword size={32} color={colors.gold} style={styles.logoIcons} />
            </View>
          </View>
          <Text style={styles.title}>HabitRPG 2.0</Text>
          <Text style={styles.subtitle}>Level up your life, one habit at a time</Text>
          <View style={styles.stars}>
            {[...Array(5)].map((_, i) => (
              <Star key={i} size={fontSize.md} color={colors.gold} fill={colors.gold} />
            ))}
          </View>
        </View>

        <View style={styles.toggleContainer}>
          <TouchableOpacity
            style={[styles.toggleButton, isLogin && styles.toggleButtonActive]}
            onPress={() => setIsLogin(true)}
          >
            <Text style={[styles.toggleText, isLogin && styles.toggleTextActive]}>Login</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.toggleButton, !isLogin && styles.toggleButtonActive]}
            onPress={() => setIsLogin(false)}
          >
            <Text style={[styles.toggleText, !isLogin && styles.toggleTextActive]}>Register</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.form}>
          {!isLogin && (
            <TextInput
              style={styles.input}
              placeholder="Username"
              value={username}
              onChangeText={setUsername}
              autoCapitalize="none"
              placeholderTextColor={colors.textSecondary}
            />
          )}
          <TextInput
            style={styles.input}
            placeholder="Email"
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
            placeholderTextColor={colors.textSecondary}
          />
          <TextInput
            style={styles.input}
            placeholder="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            placeholderTextColor={colors.textSecondary}
          />
          {error ? <Text style={{ color: "red", marginBottom: spacing.sm }}>{error}</Text> : null}
          <TouchableOpacity style={styles.submitButton} onPress={handleSubmit} disabled={isLoading}>
            {isLoading ? (
              <ActivityIndicator color={colors.textPrimary} />
            ) : (
              <Text style={styles.submitButtonText}>{isLogin ? "Enter the Realm" : "Create Account"}</Text>
            )}
          </TouchableOpacity>
        </View>

        <TouchableOpacity onPress={switchMode} style={{ marginTop: spacing.md }}>
          <Text style={{ color: colors.primary, textAlign: "center" }}>
            {isLogin ? "New adventurer? Create your character" : "Already have an account? Login"}
          </Text>
        </TouchableOpacity>
        <View style={{ marginTop: spacing.xl, flexDirection: "row", justifyContent: "space-between" }}>
          <View style={styles.features}>
            <View style={styles.star}>
              <Star size={fontSize.xl} color={colors.primary} />
            </View>
            <Text style={{ fontSize: fontSize.sm, color: colors.textSecondary }}>Earn XP</Text>
          </View>
          <View style={styles.features}>
            <View style={styles.shield}>
              <Shield size={fontSize.xl} color={colors.gold} />
            </View>
            <Text style={{ fontSize: fontSize.sm, color: colors.textSecondary }}>Level Up</Text>
          </View>
          <View style={styles.features}>
            <View style={styles.sword}>
              <Sword size={fontSize.xl} color={colors.fire} />
            </View>
            <Text style={{ fontSize: fontSize.sm, color: colors.textSecondary }}>Build Streaks</Text>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  keyboardView: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: "center",
    padding: spacing.lg,
  },
  header: {
    alignItems: "center",
    marginBottom: spacing.xxl,
  },
  logo: {
    flexDirection: "row",
    justifyContent: "center",
    alignItems: "center",
    marginTop: spacing.sm,
  },
  logoIcons: {
    position: "absolute",
    top: -5,
    right: -3,
  },
  title: {
    fontSize: fontSize.xxxl,
    fontWeight: "bold",
    color: colors.textPrimary,
    marginTop: spacing.md,
  },
  stars: {
    flexDirection: "row",
    justifyContent: "center",
    marginTop: spacing.sm,
  },
  subtitle: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
    marginTop: spacing.sm,
    textAlign: "center",
  },
  form: {
    width: "100%",
  },
  toggleContainer: {
    flexDirection: "row",
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.xs,
    marginBottom: spacing.lg,
  },
  toggleButton: {
    flex: 1,
    paddingVertical: spacing.md,
    alignItems: "center",
    borderRadius: borderRadius.md,
  },
  toggleButtonActive: {
    backgroundColor: colors.primary,
  },
  toggleText: {
    fontSize: fontSize.md,
    color: colors.textSecondary,
    fontWeight: "600",
  },
  toggleTextActive: {
    color: colors.textPrimary,
  },
  input: {
    backgroundColor: colors.cardBackground,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    fontSize: fontSize.md,
    color: colors.textPrimary,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  submitButton: {
    backgroundColor: colors.primary,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    alignItems: "center",
    marginTop: spacing.md,
  },
  submitButtonText: {
    fontSize: fontSize.lg,
    fontWeight: "bold",
    color: colors.textPrimary,
  },
  features: {
    alignItems: "center",
    flex: 1,
  },
  star: {
    width: 32,
    height: 32,
    backgroundColor: `${colors.primary}33`,
    borderRadius: 64,
    alignItems: "center",
    justifyContent: "center",
    marginLeft: "auto",
    marginRight: "auto",
  },
  shield: {
    width: 32,
    height: 32,
    backgroundColor: `${colors.gold}33`,
    borderRadius: 64,
    alignItems: "center",
    justifyContent: "center",
    marginLeft: "auto",
    marginRight: "auto",
  },
  sword: {
    width: 32,
    height: 32,
    backgroundColor: `${colors.fire}33`,
    borderRadius: 64,
    alignItems: "center",
    justifyContent: "center",
    marginLeft: "auto",
    marginRight: "auto",
  },
});
