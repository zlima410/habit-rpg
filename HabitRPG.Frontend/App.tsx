import React from "react"
import { NavigationContainer } from "@react-navigation/native"
import { createBottomTabNavigator } from "@react-navigation/bottom-tabs"
import { createStackNavigator } from "@react-navigation/stack"
import { StatusBar } from "expo-status-bar"
import { Ionicons } from "@expo/vector-icons"
import { SafeAreaProvider } from "react-native-safe-area-context"
import { ActivityIndicator, View, Text } from "react-native"

import AuthScreen from "./screens/AuthScreen"
import DashboardScreen from "./screens/DashboardScreen"
import HabitsScreen from "./screens/HabitsScreen"
import StatsScreen from "./screens/StatsScreen"
import ProfileScreen from "./screens/ProfileScreen"
import { AuthProvider, useAuth } from "./context/AuthContext"
import { colors } from "./constants/theme"
import { HabitsProvider } from "./context/HabitsContext"
import ErrorBoundaryWithReporting from "./components/ErrorBoundaryReporting"

const Tab = createBottomTabNavigator()
const Stack = createStackNavigator()

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName: keyof typeof Ionicons.glyphMap

          if (route.name === "Dashboard") {
            iconName = focused ? "home" : "home-outline"
          } else if (route.name === "Habits") {
            iconName = focused ? "checkmark-circle" : "checkmark-circle-outline"
          } else if (route.name === "Stats") {
            iconName = focused ? "stats-chart" : "stats-chart-outline"
          } else if (route.name === "Profile") {
            iconName = focused ? "person" : "person-outline"
          } else {
            iconName = "help-outline"
          }

          return <Ionicons name={iconName} size={size} color={color} />
        },
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textSecondary,
        tabBarStyle: {
          backgroundColor: colors.cardBackground,
          borderTopColor: colors.border,
          borderTopWidth: 1,
        },
        headerStyle: {
          backgroundColor: colors.background,
        },
        headerTintColor: colors.textPrimary,
        headerTitleStyle: {
          fontWeight: "bold",
        },
      })}
    >
      <Tab.Screen name="Dashboard" component={DashboardScreen} />
      <Tab.Screen name="Habits" component={HabitsScreen} />
      <Tab.Screen name="Stats" component={StatsScreen} />
      <Tab.Screen name="Profile" component={ProfileScreen} />
    </Tab.Navigator>
  )
}

function AppContent() {
    const { isAuthenticated, isInitialized } = useAuth()

    if (!isInitialized) {
        return (
        <View style={{ 
            flex: 1, 
            justifyContent: 'center', 
            alignItems: 'center', 
            backgroundColor: colors.background 
        }}>
            <ActivityIndicator size="large" color={colors.primary} />
            <Text style={{ 
            color: colors.textSecondary, 
            marginTop: 16,
            fontSize: 16
            }}>
            Loading...
            </Text>
        </View>
        )
    }

    return (
        <NavigationContainer>
        <StatusBar style="light" backgroundColor={colors.background} />
        <Stack.Navigator screenOptions={{ headerShown: false }}>
            {!isAuthenticated ? (
              <Stack.Screen name="Auth">
                {(props) => <AuthScreen {...props} onLogin={() => {}} />}
              </Stack.Screen>
            ) : (
              <Stack.Screen name="Main" component={MainTabs} />
            )}
        </Stack.Navigator>
        </NavigationContainer>
    )
}


export default function App() {
  return (
    <ErrorBoundaryWithReporting enableReporting={true}>
        <SafeAreaProvider>
        <AuthProvider>
            <HabitsProvider>
            <AppContent />
            </HabitsProvider>
        </AuthProvider>
        </SafeAreaProvider>
    </ErrorBoundaryWithReporting>
  )
}
