export type AuthenticatedUser = {
  id: number;
  accountName: string;
  firstName: string;
  lastName: string;
  roleIds: number[];
  roleCodes: string[];
  mustChangePassword: boolean;
  department: {
    id: number;
    name: string;
  };
};

export type Challenge = {
  challengeToken: string;
  maskedMobile: string;
  expiresAt: string;
  resendAvailableAt: string;
};

export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  sessionExpiresAt: string;
  user: AuthenticatedUser;
};

export type SignInProviders = {
  google: boolean;
};
