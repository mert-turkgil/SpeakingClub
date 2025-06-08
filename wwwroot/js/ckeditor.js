// Wrap all initialization in a load event to ensure CKEditor is loaded.
window.addEventListener('load', function () {
	// Ensure a tempBlogId exists in sessionStorage.
	if (!sessionStorage.getItem('tempBlogId')) {
		sessionStorage.setItem('tempBlogId', 'temp_' + Date.now());
	}
	const tempBlogId = sessionStorage.getItem('tempBlogId');

	// Destructure CKEditor 5 plugins from the global CKEDITOR object.
	const {
		ClassicEditor,
		SimpleUploadAdapter, // <<< ADD THIS!
		Alignment,
		Autoformat,
		AutoImage,
		AutoLink,
		Autosave,
		BlockQuote,
		Bold,
		Bookmark,
		CloudServices,
		Code,
		CodeBlock,
		Emoji,
		Essentials,
		FindAndReplace,
		FontBackgroundColor,
		FontColor,
		FontFamily,
		FontSize,
		FullPage,
		GeneralHtmlSupport,
		Heading,
		Highlight,
		HorizontalLine,
		HtmlComment,
		HtmlEmbed,
		ImageBlock,
		ImageCaption,
		ImageInline,
		ImageInsertViaUrl,
		ImageResize,
		ImageStyle,
		ImageTextAlternative,
		ImageToolbar,
		ImageUpload,
		Indent,
		IndentBlock,
		Italic,
		Link,
		LinkImage,
		List,
		ListProperties,
		Markdown,
		MediaEmbed,
		Mention,
		PageBreak,
		Paragraph,
		PasteFromMarkdownExperimental,
		PasteFromOffice,
		RemoveFormat,
		ShowBlocks,
		SourceEditing,
		SpecialCharacters,
		SpecialCharactersArrows,
		SpecialCharactersCurrency,
		SpecialCharactersEssentials,
		SpecialCharactersLatin,
		SpecialCharactersMathematical,
		SpecialCharactersText,
		Strikethrough,
		Style,
		Subscript,
		Superscript,
		Table,
		TableCaption,
		TableCellProperties,
		TableColumnResize,
		TableProperties,
		TableToolbar,
		TextPartLanguage,
		TextTransformation,
		Title,
		TodoList,
		Underline,
		WordCount
	} = window.CKEDITOR;

	// License key (if required). For free usage you may replace this with 'GPL'
	const LICENSE_KEY = document.querySelector('meta[name="ck-license-key"]').getAttribute('content');
    const defaultData = `<h2>Congratulations on setting up CKEditor 5! 🎉</h2>
	<p>You've successfully created a CKEditor 5 project. Enjoy editing!</p>`;
    const data = {
        Content: (document.querySelector('#Content')?.value  || '').trim() || defaultData,
        ContentUS: (document.querySelector('#ContentUS')?.value  || '').trim() || defaultData,
        ContentTR: (document.querySelector('#ContentTR')?.value  || '').trim() || defaultData,
        ContentDE: (document.querySelector('#ContentDE')?.value  || '').trim() || defaultData
    };
	// Editor configuration with simpleUpload configuration added.
	const editorConfig = {
		simpleUpload: {
			// When an image is uploaded, CKEditor will POST the file to this endpoint.
			uploadUrl: '/Admin/UploadFile?type=Images&blogId=' + encodeURIComponent(tempBlogId),
			// Optional: set additional request headers if needed.
			// headers: { 'X-CSRF-TOKEN': 'your-token' }
		},
		toolbar: {
			items: [
				'sourceEditing',
				'showBlocks',
				'findAndReplace',
				'textPartLanguage',
				'|',
				'heading',
				'style',
				'|',
				'fontSize',
				'fontFamily',
				'fontColor',
				'fontBackgroundColor',
				'|',
				'bold',
				'italic',
				'underline',
				'strikethrough',
				'subscript',
				'superscript',
				'code',
				'removeFormat',
				'|',
				'emoji',
				'specialCharacters',
				'horizontalLine',
				'pageBreak',
				'link',
				'bookmark',
				'insertImageViaUrl',
				'mediaEmbed',
				'insertTable',
				'highlight',
				'blockQuote',
				'codeBlock',
				'htmlEmbed',
				'|',
				'alignment',
				'|',
				'bulletedList',
				'numberedList',
				'todoList',
				'outdent',
				'indent'
			],
			shouldNotGroupWhenFull: false
		},
		plugins: [
			Alignment,
			Autoformat,
			AutoImage,
			AutoLink,
			Autosave,
			BlockQuote,
			Bold,
			Bookmark,
			CloudServices,
			Code,
			CodeBlock,
			Emoji,
			Essentials,
			FindAndReplace,
			FontBackgroundColor,
			FontColor,
			FontFamily,
			FontSize,
			FullPage,
			GeneralHtmlSupport,
			Heading,
			Highlight,
			HorizontalLine,
			HtmlComment,
			HtmlEmbed,
			ImageBlock,
			ImageCaption,
			ImageInline,
			ImageInsertViaUrl,
			ImageResize,
			ImageStyle,
			ImageTextAlternative,
			ImageToolbar,
			ImageUpload,
			SimpleUploadAdapter, // Added explicitly so the upload adapter is registered.
			Indent,
			IndentBlock,
			Italic,
			Link,
			LinkImage,
			List,
			ListProperties,
			Markdown,
			MediaEmbed,
			Mention,
			PageBreak,
			Paragraph,
			PasteFromMarkdownExperimental,
			PasteFromOffice,
			RemoveFormat,
			ShowBlocks,
			SourceEditing,
			SpecialCharacters,
			SpecialCharactersArrows,
			SpecialCharactersCurrency,
			SpecialCharactersEssentials,
			SpecialCharactersLatin,
			SpecialCharactersMathematical,
			SpecialCharactersText,
			Strikethrough,
			Style,
			Subscript,
			Superscript,
			Table,
			TableCaption,
			TableCellProperties,
			TableColumnResize,
			TableProperties,
			TableToolbar,
			TextPartLanguage,
			TextTransformation,
			Title,
			TodoList,
			Underline,
			WordCount
		],
		fontFamily: {
			supportAllValues: true
		},
		fontSize: {
			options: [10, 12, 14, 'default', 18, 20, 22],
			supportAllValues: true
		},
		heading: {
			options: [
				{ model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
				{ model: 'heading1', view: 'h1', title: 'Heading 1', class: 'ck-heading_heading1' },
				{ model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
				{ model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' },
				{ model: 'heading4', view: 'h4', title: 'Heading 4', class: 'ck-heading_heading4' },
				{ model: 'heading5', view: 'h5', title: 'Heading 5', class: 'ck-heading_heading5' },
				{ model: 'heading6', view: 'h6', title: 'Heading 6', class: 'ck-heading_heading6' }
			]
		},
		htmlSupport: {
			allow: [
				{
					name: /^.*$/,
					styles: true,
					attributes: true,
					classes: true
				}
			]
		},
		image: {
			toolbar: [
				'toggleImageCaption',
				'imageTextAlternative',
				'|',
				'imageStyle:inline',
				'imageStyle:wrapText',
				'imageStyle:breakText',
				'|',
				'resizeImage'
			]
		},
		initialData:
			'<h2>Congratulations on setting up CKEditor 5! 🎉</h2>\n<p>You\'ve successfully created a CKEditor 5 project. Enjoy editing!</p>',
		licenseKey: LICENSE_KEY,
		link: {
			addTargetToExternalLinks: true,
			defaultProtocol: 'https://',
			decorators: {
				toggleDownloadable: {
					mode: 'manual',
					label: 'Downloadable',
					attributes: {
						download: 'file'
					}
				}
			}
		},
		list: {
			properties: {
				styles: true,
				startIndex: true,
				reversed: true
			}
		},
		mention: {
			feeds: [
				{
					marker: '@',
					feed: [
						/* Feed items here */
					]
				}
			]
		},
		menuBar: {
			isVisible: true
		},
		placeholder: 'Type or paste your content here!',
		style: {
			definitions: [
				{ name: 'Article category', element: 'h3', classes: ['category'] },
				{ name: 'Title', element: 'h2', classes: ['document-title'] },
				{ name: 'Subtitle', element: 'h3', classes: ['document-subtitle'] },
				{ name: 'Info box', element: 'p', classes: ['info-box'] },
				{ name: 'Side quote', element: 'blockquote', classes: ['side-quote'] },
				{ name: 'Marker', element: 'span', classes: ['marker'] },
				{ name: 'Spoiler', element: 'span', classes: ['spoiler'] },
				{ name: 'Code (dark)', element: 'pre', classes: ['fancy-code', 'fancy-code-dark'] },
				{ name: 'Code (bright)', element: 'pre', classes: ['fancy-code', 'fancy-code-bright'] }
			]
		},
		table: {
			contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells', 'tableProperties', 'tableCellProperties']
		}
	};

	// Helper function to initialize an editor on a given selector.
    function createEditor(selector, storageVarName, initialContent) {
        const element = document.querySelector(selector);
        ClassicEditor.create(element, editorConfig)
            .then(editor => {
                // Set the editor's data: if initialContent is empty, use defaultData.
                if (!initialContent || initialContent.trim() === '') {
                    editor.setData(defaultData);
                } else {
                    editor.setData(initialContent);
                }
                window[storageVarName] = editor;
            })
            .catch(error => {
                console.error('Error initializing editor for', selector, error);
            });
    }
	// Create additional editor instances.
	createEditor('#Content', 'editorContent', data.Content);
	createEditor('#ContentUS', 'editorContentUS', data.ContentUS);
	createEditor('#ContentTR', 'editorContentTR', data.ContentTR);
	createEditor('#ContentDE', 'editorContentDE', data.ContentDE);

	// Initialize Swiper sliders.
	new Swiper(".categorySwiper", { slidesPerView: 3, spaceBetween: 15 });
	new Swiper(".quizSwiper", { slidesPerView: 3, spaceBetween: 15 });

	// Cover image preview function.
	function previewImage(event) {
		const reader = new FileReader();
		reader.onload = () => $('#coverPreview').attr('src', reader.result);
		reader.readAsDataURL(event.target.files[0]);
	}
	window.previewImage = previewImage;
});
