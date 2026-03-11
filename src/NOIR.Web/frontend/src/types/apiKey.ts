export interface ApiKeyDto {
  id: string
  keyIdentifier: string
  secretSuffix: string
  name: string
  description: string | null
  userId: string
  userDisplayName: string | null
  permissions: string[]
  expiresAt: string | null
  lastUsedAt: string | null
  lastUsedIp: string | null
  isRevoked: boolean
  revokedAt: string | null
  revokedReason: string | null
  isExpired: boolean
  isActive: boolean
  createdAt: string
}

export interface ApiKeyCreatedDto {
  id: string
  keyIdentifier: string
  secret: string
  name: string
  permissions: string[]
  expiresAt: string | null
  createdAt: string
}

export interface ApiKeyRotatedDto {
  id: string
  keyIdentifier: string
  secret: string
}

export interface CreateApiKeyRequest {
  name: string
  description?: string
  permissions: string[]
  expiresAt?: string
}

export interface UpdateApiKeyRequest {
  name?: string
  description?: string
  permissions?: string[]
}

export interface RevokeApiKeyRequest {
  reason?: string
}
