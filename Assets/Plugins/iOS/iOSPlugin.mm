#import "iOSPlugin.h"
using namespace std;

@implementation iOSPlugin

+ (BOOL)msalAuthWithAppId:(NSString *)appId
              andAuthority:(NSString *)authorityString    andScopes:(NSArray<NSString*>*)scopes
            andRedirectUrl:(NSString *)redirectUrlString
             andClientName:(NSString *)clientName
     withcompletionHandler:(GetTokenCompletionHandler)handler {
    
    BOOL ret = [AuthenticationManager.instance resetAuthWithAppId:appId andAuthority:authorityString andScopes:scopes andRedirectUrl:redirectUrlString andClientName:clientName];
    
    if (!ret) {
        return NO;
    }
    
    UIViewController * parent = UnityGetGLViewController();
    if (!parent) {
        return NO;
    }
    [AuthenticationManager.instance getTokenInteractivelyWithParentView:parent andCompletionBlock:^(NSString * _Nullable accessToken, NSError * _Nullable error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error || !accessToken) {
                // Show the error and stay on the sign-in page
                UIAlertController* alert = [UIAlertController
                                            alertControllerWithTitle:@"Error signing in"
                                            message:error.debugDescription
                                            preferredStyle:UIAlertControllerStyleAlert];

                UIAlertAction* okButton = [UIAlertAction
                                           actionWithTitle:@"OK"
                                           style:UIAlertActionStyleDefault
                                           handler:nil];

                [alert addAction:okButton];
                [parent presentViewController:alert animated:true completion:nil];
            }
            if (handler) {
                handler(accessToken, error.description);
            }
        });
    }];
    
    return YES;
}

@end

char* cStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    
    return res;
}

extern "C"
{
    UIViewController * _GetAppViewController() {
        UIViewController * ret = UnityGetGLViewController();
        return ret;
    }

    //public static extern bool msalAuthInteractive(string clientId, string authority, string redirect, string[] scopesArray, int scopeSize, string clientName, IntPtr callback);
    bool msalAuthInteractive(const char * clientId, const char * authority, const char * redirect, const char** scopesArray, int scopeSize, const char * clientName, const char * callbackToken, const char * callbackError) {
        
        NSMutableArray<NSString *> * scopesNSArray = [[NSMutableArray alloc] init];
        NSString * apiString = @"api://63ef8222-2e25-4b45-a42d-242e5fdff79d/access_as_user";
        //[scopesNSArray addObject:apiString];
        for (int i = 0; i < scopeSize; i++) {
            NSString * scope = [NSString stringWithUTF8String:scopesArray[i]];
            [scopesNSArray addObject:scope];
        }
        
        NSString * appId = [NSString stringWithUTF8String:clientId];
        NSString * authorityString = [NSString stringWithUTF8String:authority];
        NSString * redirectString = [NSString stringWithUTF8String:redirect];
        NSString * clientString = [NSString stringWithUTF8String:clientName];
        NSString * callBackTokenString = [NSString stringWithUTF8String:callbackToken];
        NSString * callBackErrorString = [NSString stringWithUTF8String:callbackError];
                            
        bool ret = false;
        BOOL result = [iOSPlugin msalAuthWithAppId:appId andAuthority:authorityString andScopes:scopesNSArray andRedirectUrl:redirectString andClientName:clientString withcompletionHandler:^(NSString * _Nullable accessToken, NSString * _Nullable errorMessage) {
            if (accessToken) {
                //callback(cStringCopy([accessToken UTF8String]), nullptr);
                UnitySendMessage("XcMASL", [callBackTokenString UTF8String], [accessToken UTF8String]);
            } /*else {
                //callback(nullptr, cStringCopy([errorMessage UTF8String]));
            }*/
            if (errorMessage) {
                UnitySendMessage("XcMASL", [callBackErrorString UTF8String], [errorMessage UTF8String]);
            }
        }];
        ret = result;
        return ret;
    }
}
