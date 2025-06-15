import React from 'react';

// Utility hook to get current user information
export const useCurrentUser = () => {
  // This would be replaced with an actual call to your auth system
  // For example: api.v1.auth.getCurrentUser.useQuery()
  
  // Mock implementation for demo purposes
  const mockUser = {
    id: "usr_current_user_id",
    email: "user@example.com",
    name: "Current User",
    isAuthenticated: true
  };
  
  return {
    user: mockUser,
    isLoading: false,
    error: null
  };
};

export default useCurrentUser;
