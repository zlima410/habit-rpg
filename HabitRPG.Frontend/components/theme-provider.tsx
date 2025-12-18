import * as React from 'react'
import { ReactNode } from 'react'

interface ThemeProviderProps {
    children: React.ReactNode
    defaultTheme?: string
    storageKey?: string
}

export function ThemeProvider({ children, ...props }: ThemeProviderProps) {
  return <>{children}</>;
}
