import { useQuery } from '@tanstack/react-query'
import { getBrands, getActiveBrands, getBrandById, type GetBrandsParams } from '@/services/brands'
import { brandKeys } from './queryKeys'

export const useBrandsQuery = (params: GetBrandsParams = {}) =>
  useQuery({
    queryKey: brandKeys.list(params),
    queryFn: () => getBrands(params),
  })

export const useActiveBrandsQuery = () =>
  useQuery({
    queryKey: brandKeys.active(),
    queryFn: () => getActiveBrands(),
  })

export const useBrandQuery = (id: string | undefined) =>
  useQuery({
    queryKey: brandKeys.detail(id!),
    queryFn: () => getBrandById(id!),
    enabled: !!id,
  })