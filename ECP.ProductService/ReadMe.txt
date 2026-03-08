# ═══════════════════════════════════════════════════════════════════════════════
# ECP Product Service — GraphQL Operations
# Endpoint: POST /graphql
# ═══════════════════════════════════════════════════════════════════════════════

# ─────────────────────────────────────────────────────────────────────────────
# FRAGMENTS  (reusable field sets)
# ─────────────────────────────────────────────────────────────────────────────

fragment ProductSpecFields on ProductSpec {
  key
  value
}

fragment ProductFullFields on Product {
  id
  name
  slug
  description
  brand
  categoryId
  price
  currency
  salePrice
  stockQuantity
  stockReserved
  stockAvailable
  isLowStock
  status
  tags
  images
  attributes {
    ...ProductSpecFields
  }
  version
  createdAt
  updatedAt
}

fragment ProductSummaryFields on ProductSummary {
  id
  name
  slug
  brand
  price
  currency
  salePrice
  status
  stockAvailable
  primaryImage
}

fragment PageInfoFields on PagedProductSummaryResult {
  total
  skip
  take
  hasMore
  pageCount
}


# ═══════════════════════════════════════════════════════════════════════════════
# QUERIES
# ═══════════════════════════════════════════════════════════════════════════════

# ── Get product by ID ─────────────────────────────────────────────────────────
query GetProduct($id: UUID!) {
  getProduct(id: $id) {
    ...ProductFullFields
  }
}

# Variables:
# {
#   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
# }


# ── Get product by slug ───────────────────────────────────────────────────────
query GetProductBySlug($slug: String!) {
  getProductBySlug(slug: $slug) {
    ...ProductFullFields
  }
}

# Variables:
# {
#   "slug": "apple-iphone-15-pro"
# }


# ── Get products by category (paginated) ──────────────────────────────────────
query GetProductsByCategory(
  $categoryId: UUID!
  $skip: Int! = 0
  $take: Int! = 20
) {
  getProductsByCategory(categoryId: $categoryId, skip: $skip, take: $take) {
    ...PageInfoFields
    items {
      ...ProductSummaryFields
    }
  }
}

# Variables:
# {
#   "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#   "skip": 0,
#   "take": 20
# }


# ── Search products ───────────────────────────────────────────────────────────
query SearchProducts($input: SearchProductsInput!) {
  searchProducts(input: $input) {
    ...PageInfoFields
    items {
      ...ProductSummaryFields
    }
  }
}

# Variables — full search:
# {
#   "input": {
#     "keyword": "iphone",
#     "categoryId": null,
#     "brand": "Apple",
#     "minPrice": 500,
#     "maxPrice": 2000,
#     "status": "Active",
#     "sortBy": "price",
#     "sortDesc": false,
#     "skip": 0,
#     "take": 20
#   }
# }

# Variables — keyword only:
# {
#   "input": {
#     "keyword": "laptop",
#     "skip": 0,
#     "take": 10
#   }
# }

# Variables — by brand:
# {
#   "input": {
#     "brand": "Samsung",
#     "sortBy": "price",
#     "sortDesc": true,
#     "skip": 0,
#     "take": 20
#   }
# }

# Variables — price range:
# {
#   "input": {
#     "minPrice": 100,
#     "maxPrice": 500,
#     "sortBy": "price",
#     "sortDesc": false,
#     "skip": 0,
#     "take": 20
#   }
# }


# ── Batch get products by IDs ─────────────────────────────────────────────────
query GetProductsByIds($ids: [UUID!]!) {
  getProductsByIds(ids: $ids) {
    ...ProductFullFields
  }
}

# Variables:
# {
#   "ids": [
#     "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "4fa85f64-5717-4562-b3fc-2c963f66afa7"
#   ]
# }


# ═══════════════════════════════════════════════════════════════════════════════
# MUTATIONS
# ═══════════════════════════════════════════════════════════════════════════════

# ── Create product ────────────────────────────────────────────────────────────
mutation CreateProduct($input: CreateProductInput!) {
  createProduct(input: $input) {
    ...ProductFullFields
  }
}

# Variables — minimal:
# {
#   "input": {
#     "name": "iPhone 15 Pro",
#     "description": "Apple iPhone 15 Pro 256GB",
#     "price": 999.99,
#     "currency": "USD",
#     "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "brand": "Apple",
#     "initialStock": 100
#   }
# }

# Variables — full:
# {
#   "input": {
#     "name": "iPhone 15 Pro",
#     "description": "Apple iPhone 15 Pro 256GB Natural Titanium",
#     "price": 999.99,
#     "currency": "USD",
#     "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "brand": "Apple",
#     "initialStock": 100,
#     "tags": ["smartphone", "apple", "5g"],
#     "images": [
#       "https://cdn.example.com/iphone15pro-front.jpg",
#       "https://cdn.example.com/iphone15pro-back.jpg"
#     ],
#     "attributes": [
#       { "key": "storage", "value": "256GB" },
#       { "key": "color",   "value": "Natural Titanium" },
#       { "key": "display", "value": "6.1 inch Super Retina XDR" }
#     ]
#   }
# }


# ── Update product details ────────────────────────────────────────────────────
mutation UpdateProduct($input: UpdateProductInput!) {
  updateProduct(input: $input) {
    ...ProductFullFields
  }
}

# Variables:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "name": "iPhone 15 Pro Max",
#     "description": "Apple iPhone 15 Pro Max 512GB",
#     "brand": "Apple",
#     "tags": ["smartphone", "apple", "5g", "pro-max"],
#     "images": [
#       "https://cdn.example.com/iphone15promax-front.jpg"
#     ],
#     "attributes": [
#       { "key": "storage", "value": "512GB" },
#       { "key": "color",   "value": "Black Titanium" }
#     ]
#   }
# }


# ── Update price ──────────────────────────────────────────────────────────────
mutation UpdatePrice($input: UpdatePriceInput!) {
  updatePrice(input: $input) {
    id
    name
    price
    currency
    salePrice
    updatedAt
  }
}

# Variables — regular price only:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "price": 899.99,
#     "currency": "USD"
#   }
# }

# Variables — with sale price:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "price": 999.99,
#     "currency": "USD",
#     "salePrice": 799.99
#   }
# }


# ── Adjust stock ──────────────────────────────────────────────────────────────
mutation AdjustStock($input: AdjustStockInput!) {
  adjustStock(input: $input) {
    id
    name
    stockQuantity
    stockReserved
    stockAvailable
    isLowStock
    status
    updatedAt
  }
}

# Variables — restock:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "delta": 50,
#     "reason": "Purchase order PO-2024-001 received"
#   }
# }

# Variables — consume / manual deduction:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "delta": -5,
#     "reason": "Damaged goods written off"
#   }
# }


# ── Reserve stock ─────────────────────────────────────────────────────────────
mutation ReserveStock($input: ReserveStockInput!) {
  reserveStock(input: $input) {
    id
    name
    stockQuantity
    stockReserved
    stockAvailable
    updatedAt
  }
}

# Variables:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "quantity": 2
#   }
# }


# ── Release stock ─────────────────────────────────────────────────────────────
mutation ReleaseStock($input: ReleaseStockInput!) {
  releaseStock(input: $input) {
    id
    name
    stockQuantity
    stockReserved
    stockAvailable
    updatedAt
  }
}

# Variables:
# {
#   "input": {
#     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#     "quantity": 2
#   }
# }


# ── Publish product ───────────────────────────────────────────────────────────
mutation PublishProduct($id: UUID!) {
  publishProduct(id: $id) {
    id
    name
    status
    updatedAt
  }
}

# Variables:
# {
#   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
# }


# ── Deactivate product ────────────────────────────────────────────────────────
mutation DeactivateProduct($id: UUID!) {
  deactivateProduct(id: $id) {
    id
    name
    status
    updatedAt
  }
}

# Variables:
# {
#   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
# }


# ── Archive product ───────────────────────────────────────────────────────────
mutation ArchiveProduct($id: UUID!) {
  archiveProduct(id: $id) {
    id
    name
    status
    updatedAt
  }
}

# Variables:
# {
#   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
# }


# ── Delete product ────────────────────────────────────────────────────────────
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
}

# Variables:
# {
#   "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
# }