import { createContext, useContext } from 'react';
import type { AppConfig } from './config';

export const AppConfigContext = createContext<AppConfig>({ demoMode: false });

export function useAppConfig(): AppConfig {
  return useContext(AppConfigContext);
}
