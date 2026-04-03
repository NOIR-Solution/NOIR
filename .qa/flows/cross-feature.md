# Cross-Feature Flows — Linked Data Integrity

> 8 flows testing data consistency across feature domains
> Last updated: 2026-04-03

## Flow 1: Product → Order → Payment Lifecycle
1. Create product (TC-CAT-026) → add variant (TC-CAT-029)
2. Create manual order with product (TC-ORD-036)
3. Confirm order (TC-ORD-016) → verify inventory decremented
4. Ship → Deliver → Complete (TC-ORD-017/018/019)
5. Verify payment recorded (TC-ORD-053)
6. Verify dashboard revenue updated (TC-DSH-001)
7. Verify customer order count updated (TC-CUS-027)

## Flow 2: Customer Journey
1. Create customer (TC-CUS-005)
2. Assign to customer group (TC-CUS-046)
3. Create promotion for group (TC-CUS-052)
4. Place order for customer (TC-ORD-036)
5. Verify customer detail shows order (TC-CUS-027)
6. Customer adds review (TC-CUS-064) → approve (TC-CUS-067)
7. Verify dashboard customer count (TC-DSH-001)

## Flow 3: Content Publishing
1. Upload media (TC-CON-059)
2. Create blog category (TC-CON-041)
3. Create blog tag (TC-CON-051)
4. Create blog post with uploaded media, category, tag (TC-CON-018)
5. Use Tiptap editor for rich content (TC-CON-019/020)
6. Publish post (TC-CON-023)
7. Verify post appears in list with correct category/tags (TC-CON-001)

## Flow 4: HR Workflow
1. Create department (TC-HR-031)
2. Create employee tags (TC-HR-040)
3. Create employee in department (TC-HR-002)
4. Assign tags via TagSelector (TC-HR-039)
5. Verify department employee count (TC-HR-029)
6. Verify org chart shows employee (TC-HR-047)
7. Export employees CSV → verify employee included (TC-HR-012)

## Flow 5: CRM Pipeline
1. Create company (TC-CRM-023)
2. Create contact linked to company (TC-CRM-002)
3. Create lead in pipeline (TC-CRM-040)
4. Drag lead between stages (TC-CRM-037)
5. Win lead (TC-CRM-038)
6. Verify deal detail (TC-CRM-048)
7. Verify company detail shows deal (TC-CRM-030)

## Flow 6: PM Workflow
1. Create project (TC-PM-003)
2. Create Kanban columns (project detail)
3. Quick-add tasks (TC-PM-018)
4. Drag tasks between columns (TC-PM-017)
5. Open task detail → add subtask (TC-PM-052) + comment (TC-PM-050)
6. Complete subtask → verify parent progress
7. Verify project list shows updated task count

## Flow 7: Data Integrity & Edge Cases
1. Delete product → verify orders referencing it still display (soft delete)
2. Disable module → verify sidebar hides, dashboard widget hides
3. Re-enable module → verify everything returns
4. Change language → verify all pages render Vietnamese
5. Switch theme → verify dark mode across 5 random pages

## Flow 8: Error Handling & Recovery
1. Submit form with invalid data → verify inline errors (not toast)
2. Navigate to nonexistent entity URL → verify 404/redirect
3. Double-click submit → verify no duplicate creation
4. Session timeout → verify redirect to login
5. Server error → verify FormErrorBanner displays
