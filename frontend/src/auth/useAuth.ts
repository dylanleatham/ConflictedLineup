import { useContext } from 'react';
import { AuthContext } from './AuthContext';
import { AuthState } from './types';

export function useAuth(): AuthState {
  const auth = useContext(AuthContext);
  if (!auth) {
    throw new Error('useAuth must be used inside an AuthProvider');
  }
  return auth;
}
