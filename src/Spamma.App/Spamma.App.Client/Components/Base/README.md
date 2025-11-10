# Modal and Slideout Base Components

Shared base components for consistent modal and slideout (side panel) UI across Spamma.

## Components

### ModalBase.razor

Centered modal dialog with consistent styling. Used for confirmations, forms in modal context, and alerts.

**Features:**
- Centered on-screen with backdrop overlay
- Configurable width (sm, md, lg, xl, 2xl, 3xl, 4xl)
- Optional background-click-to-close (default: disabled for confirmations)
- Tailwind + backdrop blur styling
- Accessibility support (ARIA labels)

**Parameters:**
- `IsVisible` (bool): Controls modal visibility
- `OnClose` (EventCallback): Fired when modal should close
- `AllowBackdropClose` (bool): If true, clicking backdrop closes modal (default: false)
- `WidthClass` (string): Tailwind width class (default: "sm:max-w-sm w-full")
- `AriaLabelledBy` (string?): ARIA label ID for accessibility
- `ChildContent` (RenderFragment?): Modal content (header, body, footer)

**Example: Confirmation Modal**
```razor
<ModalBase IsVisible="showConfirmModal" 
           OnClose="@(() => showConfirmModal = false)"
           AllowBackdropClose="false"
           WidthClass="sm:max-w-md w-full"
           AriaLabelledBy="confirm-title">
    <div class="px-6 py-4 border-b border-gray-200">
        <h3 id="confirm-title" class="text-lg font-medium text-gray-900">Confirm Delete</h3>
    </div>
    <div class="px-6 py-4">
        <p>Are you sure you want to delete this item? This action cannot be undone.</p>
    </div>
    <div class="px-6 py-4 border-t border-gray-200 flex justify-end space-x-3">
        <button @onclick="() => showConfirmModal = false" class="px-4 py-2 text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">
            Cancel
        </button>
        <button @onclick="ConfirmDelete" class="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700">
            Delete
        </button>
    </div>
</ModalBase>
```

**Example: Form Modal with Backdrop Close**
```razor
<ModalBase IsVisible="showEditModal" 
           OnClose="@(() => showEditModal = false)"
           AllowBackdropClose="true"
           WidthClass="sm:max-w-lg w-full"
           AriaLabelledBy="edit-title">
    <div class="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
        <h3 id="edit-title" class="text-lg font-medium text-gray-900">Edit Item</h3>
        <button @onclick="() => showEditModal = false" class="text-gray-400 hover:text-gray-600">
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
        </button>
    </div>
    <!-- Form content here -->
</ModalBase>
```

---

### SlideoutBase.razor

Side panel that slides in from left or right edge. Used for detailed forms, editing, and complex interactions.

**Features:**
- Slides from left or right edge with smooth animation
- Configurable width (sm, md, lg, xl, 2xl, 3xl, 4xl, 5xl)
- Direction control (left/right)
- Optional background-click-to-close (default: enabled)
- Backdrop blur styling
- Accessibility support (ARIA labels)
- Prevents pointer events on unused space

**Parameters:**
- `IsVisible` (bool): Controls slideout visibility
- `OnClose` (EventCallback): Fired when slideout should close
- `Direction` (string): "left" (default) or "right"
- `AllowBackdropClose` (bool): If true, clicking backdrop closes slideout (default: true)
- `WidthClass` (string): Tailwind width class (default: "max-w-md")
- `AriaLabelledBy` (string?): ARIA label ID for accessibility
- `ChildContent` (RenderFragment?): Slideout content (header, body, footer)

**Example: Edit Slideout**
```razor
<SlideoutBase IsVisible="showEditSlideout" 
              OnClose="@(() => showEditSlideout = false)"
              Direction="right"
              AllowBackdropClose="true"
              WidthClass="max-w-md"
              AriaLabelledBy="edit-slideout-title">
    <!-- Header -->
    <div class="flex items-center justify-between bg-blue-600 px-4 py-6 sm:px-6">
        <h2 id="edit-slideout-title" class="text-lg font-medium text-white">Edit Item</h2>
        <button @onclick="() => showEditSlideout = false" class="text-blue-200 hover:text-white">
            <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
        </button>
    </div>

    <!-- Body -->
    <div class="flex-1 overflow-y-auto px-4 py-6 sm:px-6">
        <EditForm Model="@item" OnSubmit="@SaveChanges">
            <!-- Form fields here -->
        </EditForm>
    </div>

    <!-- Footer -->
    <div class="border-t border-gray-200 px-4 py-4 sm:px-6 flex justify-end space-x-3">
        <button @onclick="() => showEditSlideout = false" class="px-4 py-2 text-gray-700 border border-gray-300 rounded-md hover:bg-gray-50">
            Cancel
        </button>
        <button @onclick="SaveChanges" class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
            Save Changes
        </button>
    </div>
</SlideoutBase>
```

---

## Styling

Both components use:
- **Backdrop**: `bg-black bg-opacity-50 backdrop-blur-sm` for modern, clean overlay
- **Animations**: Smooth CSS transitions for opacity and transform
- **Z-index**: Appropriate stacking (modal z-50, slideout z-40)
- **Accessibility**: ARIA roles and labels for screen readers

## Migration from Existing Modals

To migrate existing inline modals to use these base components:

1. **Extract the modal/slideout structure** into the base component
2. **Move header/body/footer content** into `ChildContent`
3. **Update parameters** (IsVisible, OnClose, etc.)
4. **Remove duplicate styling** (backdrop, positioning, animations)
5. **Test backdrop click behavior** and set `AllowBackdropClose` appropriately

### Before (Inline Modal)
```razor
@if (showModal)
{
    <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity z-50 flex items-center justify-center p-4" @onclick="CloseModal">
        <div class="bg-white rounded-lg p-6 max-w-md w-full">
            <h3>Title</h3>
            <!-- content -->
        </div>
    </div>
}
```

### After (Using ModalBase)
```razor
<ModalBase IsVisible="showModal" 
           OnClose="@(() => showModal = false)"
           WidthClass="sm:max-w-md w-full">
    <div class="px-6 py-4">
        <h3>Title</h3>
        <!-- content -->
    </div>
</ModalBase>
```

## Benefits

✅ **Consistency**: All modals/slideouts use the same styling and behavior  
✅ **Maintainability**: Single source of truth for overlay styling  
✅ **Accessibility**: Built-in ARIA support  
✅ **Flexibility**: Parameters allow customization without changing core component  
✅ **Animation**: Smooth, professional transitions  
✅ **Performance**: Efficient pointer-events management  
