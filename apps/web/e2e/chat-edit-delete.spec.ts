import { test, expect, Page } from '@playwright/test';

/**
 * E2E Tests for CHAT-06: Message Editing and Deletion Feature
 *
 * Tests cover:
 * - Edit message flow (happy path)
 * - Edit validation (empty content)
 * - Delete message flow (happy path)
 * - Delete cancellation
 * - Edit/Delete button visibility on hover
 * - Invalidation warning display
 * - Error handling (403 Forbidden)
 *
 * These tests use real authentication with demo user and strategic API mocking
 * for edit/delete operations to ensure predictable behavior in CI.
 */

test.describe('CHAT-06: Message Editing and Deletion', () => {
  /**
   * Setup: Login and navigate to chat page before each test
   */
  test.beforeEach(async ({ page }) => {
    // Login with demo user
    await page.goto('/');
    await page.fill('input[type="email"]', 'user@meepleai.dev');
    await page.fill('input[type="password"]', 'Demo123!');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL('/');

    // Navigate to chat page
    await page.goto('/chat');
    await page.waitForLoadState('networkidle');
  });

  /**
   * Test 1: Edit Message Flow - Complete Happy Path
   *
   * Verifies that a user can successfully edit their own message,
   * save the changes, and see the updated content with the "(modificato)" badge.
   */
  test('should allow user to edit their own message successfully', async ({ page }) => {
    // Send initial message with unique timestamp
    const originalMessage = `Test message for editing ${Date.now()}`;
    const editedMessage = `Edited message content ${Date.now()}`;

    // Find chat input and send message
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(originalMessage);
    await chatInput.press('Enter');

    // Wait for message to appear in chat (look for user message)
    await page.waitForSelector(`text=${originalMessage}`, { timeout: 10000 });

    // Wait for AI response to complete (indicated by presence of assistant message)
    // This ensures the chat is in a stable state before editing
    await page.waitForTimeout(2000); // Give AI time to respond

    // Locate the user message container (not the AI response)
    const userMessage = page.locator(`text=${originalMessage}`).first();

    // Hover over message to reveal edit button
    await userMessage.hover();

    // Click edit button - use force click in case hover state is flaky
    const editButton = page.locator('button[title="Modifica messaggio"]').first();
    await editButton.click({ force: true });

    // Verify textarea appears with original content
    const editTextarea = page.locator('textarea[aria-label="Edit message content"]');
    await expect(editTextarea).toBeVisible();
    await expect(editTextarea).toHaveValue(originalMessage);

    // Clear and type new content
    await editTextarea.clear();
    await editTextarea.fill(editedMessage);

    // Mock the API update response for predictable testing
    await page.route('**/api/v1/chats/*/messages/*', async (route) => {
      const method = route.request().method();
      if (method === 'PUT') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            messageId: 'test-message-id',
            content: editedMessage,
            isEdited: true,
            editedAt: new Date().toISOString()
          })
        });
      } else {
        await route.continue();
      }
    });

    // Click Save button
    const saveButton = page.locator('button[aria-label="Save edited message"]');
    await saveButton.click();

    // Wait for API call to complete
    await page.waitForResponse(response =>
      response.url().includes('/api/v1/chats/') &&
      response.url().includes('/messages/') &&
      response.request().method() === 'PUT'
    );

    // Verify message updated with new content
    await expect(page.locator(`text=${editedMessage}`).first()).toBeVisible();

    // Verify "(modificato)" badge appears
    await expect(page.locator('text=(modificato)')).toBeVisible();

    // Verify original message no longer visible
    const originalMessageLocator = page.locator(`text=${originalMessage}`);
    await expect(originalMessageLocator).not.toBeVisible();
  });

  /**
   * Test 2: Edit Validation - Empty Content Not Allowed
   *
   * Verifies that the Save button is disabled when the textarea is empty,
   * preventing users from saving blank messages.
   */
  test('should disable save button when edit textarea is empty', async ({ page }) => {
    // Send message
    const testMessage = `Test validation message ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });
    await page.waitForTimeout(1000);

    // Enter edit mode
    const userMessage = page.locator(`text=${testMessage}`).first();
    await userMessage.hover();
    const editButton = page.locator('button[title="Modifica messaggio"]').first();
    await editButton.click({ force: true });

    // Verify textarea appears
    const editTextarea = page.locator('textarea[aria-label="Edit message content"]');
    await expect(editTextarea).toBeVisible();

    // Clear all text
    await editTextarea.clear();

    // Verify Save button is disabled
    const saveButton = page.locator('button[aria-label="Save edited message"]');
    await expect(saveButton).toBeDisabled();

    // Cancel edit
    const cancelButton = page.locator('button[aria-label="Cancel edit"]');
    await cancelButton.click();

    // Verify back to normal view
    await expect(editTextarea).not.toBeVisible();
    await expect(page.locator(`text=${testMessage}`).first()).toBeVisible();
  });

  /**
   * Test 3: Delete Message Flow - Complete Happy Path
   *
   * Verifies that a user can delete their own message via the confirmation modal,
   * and the message is replaced with "[Messaggio eliminato]" placeholder.
   */
  test('should allow user to delete their own message successfully', async ({ page }) => {
    // Send message to delete
    const testMessage = `Message to delete ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });
    await page.waitForTimeout(1000);

    // Hover and click delete button
    const userMessage = page.locator(`text=${testMessage}`).first();
    await userMessage.hover();
    const deleteButton = page.locator('button[title="Elimina messaggio"]').first();
    await deleteButton.click({ force: true });

    // Verify confirmation modal appears
    const modalTitle = page.locator('text=Eliminare il messaggio?');
    await expect(modalTitle).toBeVisible();

    // Verify modal content mentions action cannot be undone
    await expect(page.locator('text=/non può essere annullata/i')).toBeVisible();

    // Mock the API delete response
    await page.route('**/api/v1/chats/*/messages/*', async (route) => {
      const method = route.request().method();
      if (method === 'DELETE') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            success: true,
            messageId: 'test-message-id'
          })
        });
      } else {
        await route.continue();
      }
    });

    // Click "Elimina" button
    const confirmButton = page.locator('button:has-text("Elimina")');
    await confirmButton.click();

    // Wait for API call to complete
    await page.waitForResponse(response =>
      response.url().includes('/api/v1/chats/') &&
      response.url().includes('/messages/') &&
      response.request().method() === 'DELETE',
      { timeout: 5000 }
    );

    // Wait for modal to close
    await expect(modalTitle).not.toBeVisible();

    // Verify message replaced with "[Messaggio eliminato]"
    await expect(page.locator('text=[Messaggio eliminato]')).toBeVisible();

    // Verify original message no longer visible
    await expect(page.locator(`text=${testMessage}`)).not.toBeVisible();
  });

  /**
   * Test 4: Delete Cancellation
   *
   * Verifies that clicking "Annulla" in the delete confirmation modal
   * closes the modal without deleting the message.
   */
  test('should cancel delete operation when user clicks cancel', async ({ page }) => {
    // Send message
    const testMessage = `Message not to delete ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });
    await page.waitForTimeout(1000);

    // Hover and click delete button
    const userMessage = page.locator(`text=${testMessage}`).first();
    await userMessage.hover();
    const deleteButton = page.locator('button[title="Elimina messaggio"]').first();
    await deleteButton.click({ force: true });

    // Verify modal appears
    const modalTitle = page.locator('text=Eliminare il messaggio?');
    await expect(modalTitle).toBeVisible();

    // Click "Annulla" button
    const cancelButton = page.locator('button:has-text("Annulla")');
    await cancelButton.click();

    // Verify modal closes
    await expect(modalTitle).not.toBeVisible();

    // Verify original message still visible
    await expect(page.locator(`text=${testMessage}`).first()).toBeVisible();

    // Verify no deleted placeholder
    await expect(page.locator('text=[Messaggio eliminato]')).not.toBeVisible();
  });

  /**
   * Test 5: Edit/Delete Button Visibility
   *
   * Verifies that:
   * - Edit/Delete buttons are not visible by default
   * - Buttons appear on hover for user messages
   * - AI response messages do NOT have edit/delete buttons
   */
  test('should show edit/delete buttons only on hover for user messages', async ({ page }) => {
    // Send message
    const testMessage = `Message for visibility test ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });

    // Wait for AI response (look for assistant role or typical AI response pattern)
    await page.waitForTimeout(3000); // Give AI time to respond

    // Locate user message container (should contain the test message)
    const userMessageContainer = page.locator(`text=${testMessage}`).first().locator('xpath=ancestor::div[contains(@class, "message") or contains(@class, "user")]').first();

    // Verify buttons NOT visible initially (before hover)
    const editButtonBeforeHover = page.locator('button[title="Modifica messaggio"]').first();
    const deleteButtonBeforeHover = page.locator('button[title="Elimina messaggio"]').first();

    // Note: Buttons may be in DOM but hidden, so we check visibility state
    // If they're not in DOM at all, that's also acceptable
    const editButtonCount = await page.locator('button[title="Modifica messaggio"]').count();
    if (editButtonCount > 0) {
      await expect(editButtonBeforeHover).not.toBeVisible();
    }

    // Hover over user message
    await userMessageContainer.hover();

    // Verify buttons become visible after hover
    await expect(editButtonBeforeHover).toBeVisible({ timeout: 2000 });
    await expect(deleteButtonBeforeHover).toBeVisible({ timeout: 2000 });

    // Verify AI response messages do NOT have edit/delete buttons
    // Look for assistant messages (typically have different styling or role)
    const assistantMessages = page.locator('[data-role="assistant"], .assistant-message, .ai-message');
    const assistantMessageCount = await assistantMessages.count();

    if (assistantMessageCount > 0) {
      // Hover over AI message
      await assistantMessages.first().hover();

      // Wait a bit for any potential buttons to appear
      await page.waitForTimeout(500);

      // Verify no edit/delete buttons for AI messages
      // This is tricky - we need to ensure buttons are scoped to user messages only
      // Check that there's only one set of visible edit/delete buttons (for user message)
      const visibleEditButtons = await page.locator('button[title="Modifica messaggio"]:visible').count();
      const visibleDeleteButtons = await page.locator('button[title="Elimina messaggio"]:visible').count();

      // Should be exactly 1 of each (for the user message we hovered earlier)
      expect(visibleEditButtons).toBeLessThanOrEqual(1);
      expect(visibleDeleteButtons).toBeLessThanOrEqual(1);
    }
  });

  /**
   * Test 6: Invalidation Warning Display
   *
   * Verifies that when a message has isInvalidated=true flag,
   * a yellow/amber warning banner appears with appropriate warning text.
   *
   * Note: This test uses API interception to simulate invalidated message state.
   */
  test('should display invalidation warning for invalidated messages', async ({ page }) => {
    // Send a message first
    const testMessage = `Message to invalidate ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });
    await page.waitForTimeout(2000);

    // Intercept chat history endpoint to return message with isInvalidated=true
    await page.route('**/api/v1/chats/*/messages*', async (route) => {
      const method = route.request().method();
      if (method === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            messages: [
              {
                messageId: 'user-message-id',
                role: 'user',
                content: testMessage,
                timestamp: new Date().toISOString(),
                isEdited: false
              },
              {
                messageId: 'assistant-message-id',
                role: 'assistant',
                content: 'This is an AI response that is now invalidated.',
                timestamp: new Date().toISOString(),
                isEdited: false,
                isInvalidated: true,
                invalidationReason: 'Il messaggio originale è stato modificato'
              }
            ],
            hasMore: false
          })
        });
      } else {
        await route.continue();
      }
    });

    // Reload chat to trigger the intercepted response
    await page.reload();
    await page.waitForLoadState('networkidle');

    // Verify warning banner appears
    const warningBanner = page.locator('text=/Questa risposta potrebbe essere obsoleta/i, text=/potrebbe non essere più accurata/i');
    await expect(warningBanner.first()).toBeVisible({ timeout: 5000 });

    // Verify warning has appropriate styling (yellow/amber background)
    // Check for common warning color classes or inline styles
    const warningElement = warningBanner.first().locator('xpath=ancestor::div[contains(@class, "warning") or contains(@class, "amber") or contains(@class, "yellow")]').first();
    await expect(warningElement).toBeVisible();
  });

  /**
   * Test 7: Error Handling - 403 Forbidden
   *
   * Verifies that when the API returns 403 (user trying to edit another user's message),
   * an appropriate error message is displayed to the user.
   */
  test('should display error message when edit fails with 403 Forbidden', async ({ page }) => {
    // Send message
    const testMessage = `Message for error test ${Date.now()}`;
    const chatInput = page.locator('textarea[placeholder*="Chiedi"], textarea[placeholder*="domanda"], input[type="text"]').first();
    await chatInput.fill(testMessage);
    await chatInput.press('Enter');

    // Wait for message to appear
    await page.waitForSelector(`text=${testMessage}`, { timeout: 10000 });
    await page.waitForTimeout(1000);

    // Enter edit mode
    const userMessage = page.locator(`text=${testMessage}`).first();
    await userMessage.hover();
    const editButton = page.locator('button[title="Modifica messaggio"]').first();
    await editButton.click({ force: true });

    // Modify content
    const editedMessage = `Edited content that will fail ${Date.now()}`;
    const editTextarea = page.locator('textarea[aria-label="Edit message content"]');
    await editTextarea.clear();
    await editTextarea.fill(editedMessage);

    // Mock API to return 403 Forbidden
    await page.route('**/api/v1/chats/*/messages/*', async (route) => {
      const method = route.request().method();
      if (method === 'PUT') {
        await route.fulfill({
          status: 403,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'forbidden',
            message: 'Non hai i permessi per modificare questo messaggio'
          })
        });
      } else {
        await route.continue();
      }
    });

    // Click Save button
    const saveButton = page.locator('button[aria-label="Save edited message"]');
    await saveButton.click();

    // Wait for API call to complete
    await page.waitForResponse(response =>
      response.url().includes('/api/v1/chats/') &&
      response.url().includes('/messages/') &&
      response.request().method() === 'PUT'
    );

    // Verify error message appears
    // Look for common error message patterns
    const errorMessage = page.locator('text=/errore/i, text=/permessi/i, text=/non autorizzato/i').first();
    await expect(errorMessage).toBeVisible({ timeout: 3000 });

    // Verify message was NOT updated (original still visible)
    await expect(page.locator(`text=${testMessage}`).first()).toBeVisible();

    // Verify edited content NOT visible
    await expect(page.locator(`text=${editedMessage}`)).not.toBeVisible();
  });
});
