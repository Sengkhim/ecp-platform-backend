# ═══════════════════════════════════════════════════════════════════════════════
# ECP Product Service — GraphQL Operations
# Endpoint: POST /graphql
# ═══════════════════════════════════════════════════════════════════════════════

# ─────────────────────────────────────────────────────────────────────────────
# FRAGMENTS
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
  attributes { ...ProductSpecFields }
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
  product(id: $id) {
    ...ProductFullFields
  }
}
# { "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }


# ── Get product by slug ───────────────────────────────────────────────────────
query GetProductBySlug($slug: String!) {
  productBySlug(slug: $slug) {
    ...ProductFullFields
  }
}
# { "slug": "apple-iphone-15-pro" }


# ── Get ALL products (paginated) ──────────────────────────────────────────────
query GetAllProducts($skip: Int = 0, $take: Int = 20) {
  products(skip: $skip, take: $take) {
    ...PageInfoFields
    items {
      ...ProductSummaryFields
    }
  }
}
# { "skip": 0, "take": 20 }


# ── Get products by category ──────────────────────────────────────────────────
query GetProductsByCategory($categoryId: UUID!, $skip: Int = 0, $take: Int = 20) {
  productsByCategory(categoryId: $categoryId, skip: $skip, take: $take) {
    ...PageInfoFields
    items {
      ...ProductSummaryFields
    }
  }
}
# { "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "skip": 0, "take": 20 }


# ── Search products ───────────────────────────────────────────────────────────
query SearchProducts($input: SearchProductsInput!) {
  searchProducts(input: $input) {
    ...PageInfoFields
    items {
      ...ProductSummaryFields
    }
  }
}
# Full:
# { "input": { "keyword": "iphone", "brand": "Apple", "minPrice": 500,
#              "maxPrice": 2000, "status": "Active", "sortBy": "price",
#              "sortDesc": false, "skip": 0, "take": 20 } }
# Keyword only:
# { "input": { "keyword": "laptop" } }
# Price range:
# { "input": { "minPrice": 100, "maxPrice": 500, "sortBy": "price", "sortDesc": false } }
# Active only:
# { "input": { "status": "Active", "sortBy": "createdAt", "sortDesc": true } }


# ── Batch get by IDs ──────────────────────────────────────────────────────────
query GetProductsByIds($ids: [UUID!]!) {
  productsByIds(ids: $ids) {
    ...ProductFullFields
  }
}
# { "ids": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "4fa85f64-5717-4562-b3fc-2c963f66afa7"] }


# ═══════════════════════════════════════════════════════════════════════════════
# MUTATIONS
# ═══════════════════════════════════════════════════════════════════════════════

# ── Create product ────────────────────────────────────────────────────────────
mutation CreateProduct($input: CreateProductInput!) {
  createProduct(input: $input) {
    ...ProductFullFields
  }
}
# Minimal:
# { "input": { "name": "iPhone 15 Pro", "description": "Apple iPhone 15 Pro 256GB",
#              "price": 999.99, "currency": "USD",
#              "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#              "brand": "Apple", "initialStock": 100 } }
# Full:
# { "input": { "name": "iPhone 15 Pro", "description": "Apple iPhone 15 Pro 256GB",
#              "price": 999.99, "currency": "USD",
#              "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#              "brand": "Apple", "initialStock": 100,
#              "tags": ["smartphone", "apple", "5g"],
#              "images": ["https://cdn.example.com/img1.jpg"],
#              "attributes": [{ "key": "storage", "value": "256GB" },
#                             { "key": "color",   "value": "Natural Titanium" }] } }


# ── Update product ────────────────────────────────────────────────────────────
mutation UpdateProduct($input: UpdateProductInput!) {
  updateProduct(input: $input) {
    ...ProductFullFields
  }
}
# { "input": { "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
#              "name": "iPhone 15 Pro Max", "description": "Updated desc",
#              "brand": "Apple", "tags": ["smartphone", "pro-max"],
#              "attributes": [{ "key": "storage", "value": "512GB" }] } }


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
# Regular:   { "input": { "id": "...", "price": 899.99, "currency": "USD" } }
# With sale: { "input": { "id": "...", "price": 999.99, "currency": "USD", "salePrice": 799.99 } }


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
# Restock:  { "input": { "id": "...", "delta": 50,  "reason": "PO-2024-001 received" } }
# Write-off:{ "input": { "id": "...", "delta": -5,  "reason": "Damaged goods" } }


# ── Reserve stock ─────────────────────────────────────────────────────────────
mutation ReserveStock($input: ReserveStockInput!) {
  reserveStock(input: $input) {
    id
    stockQuantity
    stockReserved
    stockAvailable
    updatedAt
  }
}
# { "input": { "id": "...", "quantity": 2 } }


# ── Release stock ─────────────────────────────────────────────────────────────
mutation ReleaseStock($input: ReleaseStockInput!) {
  releaseStock(input: $input) {
    id
    stockQuantity
    stockReserved
    stockAvailable
    updatedAt
  }
}
# { "input": { "id": "...", "quantity": 2 } }


# ── Status transitions ────────────────────────────────────────────────────────
mutation PublishProduct($id: UUID!) {
  publishProduct(id: $id) { id name status updatedAt }
}

mutation DeactivateProduct($id: UUID!) {
  deactivateProduct(id: $id) { id name status updatedAt }
}

mutation ArchiveProduct($id: UUID!) {
  archiveProduct(id: $id) { id name status updatedAt }
}


# ── Delete product ────────────────────────────────────────────────────────────
mutation DeleteProduct($id: UUID!) {
  deleteProduct(id: $id)
}
# { "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }