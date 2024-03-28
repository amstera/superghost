extern UIViewController* UnityGetGLViewController();

extern "C" void _Native_Share_iOS(const char* message)
{
    NSMutableArray *items = [NSMutableArray new];
    NSString *shareMessage = [NSString stringWithUTF8String:message];

    // Assuming you want to share a text message, we add it directly to the items array
    if(shareMessage != nil)
        [items addObject:shareMessage];

    UIActivityViewController *activity = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
    UIViewController *controller = UnityGetGLViewController();
    controller.modalPresentationStyle = UIModalPresentationPopover;
    controller.popoverPresentationController.sourceView = controller.view;
    controller.popoverPresentationController.sourceRect = CGRectMake(controller.view.frame.size.width / 2, controller.view.frame.size.height / 4, 0, 0);
    
    // For iPads, the presentation of the activity view controller is slightly different to avoid crashes
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
        activity.popoverPresentationController.sourceView = controller.view;
        activity.popoverPresentationController.sourceRect = CGRectMake(controller.view.frame.size.width / 2, controller.view.frame.size.height / 4, 0, 0);
    }
    
    [controller presentViewController:activity animated:YES completion:nil];
}