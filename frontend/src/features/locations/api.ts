import { apiClient } from '../../api/client'
import type { ApiResponse } from '../../types'
import { unwrap } from '../../utils/api'

export type LocationItem = {
  code: string
  name: string
}

export const locationsApi = {
  provinces: () => unwrap(apiClient.get<ApiResponse<LocationItem[]>>('/locations/provinces')),
  districts: (provinceCode: string) =>
    unwrap(
      apiClient.get<ApiResponse<LocationItem[]>>('/locations/districts', {
        params: { provinceCode },
      }),
    ),
  wards: (districtCode: string) =>
    unwrap(
      apiClient.get<ApiResponse<LocationItem[]>>('/locations/wards', {
        params: { districtCode },
      }),
    ),
}
