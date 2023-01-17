//
//  AuthenticationManager.m
//  GraphTutorial
//
//  Copyright (c) Microsoft. All rights reserved.
//  Licensed under the MIT license.
//

// <AuthManagerSnippet>
#import "AuthenticationManager.h"

@interface AuthenticationManager()

@property NSString* appId;
@property NSArray<NSString*>* graphScopes;

@property NSString * redirectUrlString;
@property NSString * authorityString;
@property NSString * clientName;

@property MSALPublicClientApplication* publicClient;

@end

@implementation AuthenticationManager

+ (id) instance {
    static AuthenticationManager *singleInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^ {
        singleInstance = [[self alloc] init];
    });

    return singleInstance;
}

- (id) init {
    if (self = [super init]) {
        //DO NOTHING.
        /*
        // Get app ID and scopes from AuthSettings.plist
        NSString* authConfigPath =
        [NSBundle.mainBundle pathForResource:@"AuthSettings" ofType:@"plist"];
        NSDictionary* authConfig = [NSDictionary dictionaryWithContentsOfFile:authConfigPath];

        self.appId = authConfig[@"AppId"];
        self.graphScopes = authConfig[@"GraphScopes"];

        // Create the MSAL client
        self.publicClient = [[MSALPublicClientApplication alloc] initWithClientId:self.appId error:nil];*/
    }

    return self;
}

- (BOOL)resetAuthWithAppId:(NSString *)appId
              andAuthority:(NSString *)authorityString    andScopes:(NSArray<NSString*>*)scopes
            andRedirectUrl:(NSString *)redirectUrlString
             andClientName:(NSString *)clientName
{
    BOOL ret = NO;
    
    if (!(appId
          && authorityString
          && scopes
          && redirectUrlString)) {
        return NO;
    }
    
    self.appId = appId;
    self.authorityString = authorityString;
    self.graphScopes = scopes;
    self.redirectUrlString = redirectUrlString;
    self.clientName = clientName;
    
    ret = [self doInit];
    return ret;
}

- (BOOL)doInit {
    BOOL ret = NO;
    
    NSURL * authorityUrl = [[NSURL alloc] initWithString:self.authorityString];
    MSALAuthority * msalAuthority = [MSALAuthority authorityWithURL:authorityUrl error:nil];
    if (!msalAuthority) {
        return NO;
    }
    MSALPublicClientApplicationConfig * config = [[MSALPublicClientApplicationConfig alloc] initWithClientId:self.appId redirectUri:self.redirectUrlString authority:msalAuthority];
    if (!config) {
        return NO;
    }
    if (self.publicClient) {
        self.publicClient = nil;
    }
    self.publicClient = [[MSALPublicClientApplication alloc] initWithConfiguration:config error:nil];
    if (!self.publicClient) {
        ret = FALSE;
    }

    return ret;
}

- (void) getAccessTokenForProviderOptions:(id<MSAuthenticationProviderOptions>) authProviderOptions
                            andCompletion:(void (^)(NSString * _Nonnull, NSError * _Nonnull)) completion {
    [self getTokenSilentlyWithCompletionBlock:completion];
}

- (void) getTokenInteractivelyWithParentView:(UIViewController *) parentView
                          andCompletionBlock:(GetTokenCompletionBlock) completionBlock {
    MSALWebviewParameters* webParameters = [[MSALWebviewParameters alloc]
                                            initWithAuthPresentationViewController:parentView];
    MSALInteractiveTokenParameters* interactiveParameters =
    [[MSALInteractiveTokenParameters alloc]
     initWithScopes:self.graphScopes
     webviewParameters:webParameters];

    // Call acquireToken to open a browser so the user can sign in
    [self.publicClient
     acquireTokenWithParameters:interactiveParameters
     completionBlock:^(MSALResult * _Nullable result, NSError * _Nullable error) {

        // Check error
        if (error) {
            completionBlock(nil, error);
            return;
        }

        // Check result
        if (!result) {
            NSMutableDictionary* details = [NSMutableDictionary dictionary];
            [details setValue:@"No result was returned" forKey:NSDebugDescriptionErrorKey];
            completionBlock(nil, [NSError errorWithDomain:@"AuthenticationManager" code:0 userInfo:details]);
            return;
        }

        NSLog(@"Got token interactively: %@", result.accessToken);
        completionBlock(result.accessToken, nil);
    }];
}

- (void) getTokenSilentlyWithCompletionBlock:(GetTokenCompletionBlock)completionBlock {
    // Check if there is an account in the cache
    NSError* msalError;
    MSALAccount* account = [self.publicClient allAccounts:&msalError].firstObject;

    if (msalError || !account) {
        NSMutableDictionary* details = [NSMutableDictionary dictionary];
        [details setValue:@"Could not retrieve account from cache" forKey:NSDebugDescriptionErrorKey];
        completionBlock(nil, [NSError errorWithDomain:@"AuthenticationManager" code:0 userInfo:details]);
        return;
    }

    MSALSilentTokenParameters* silentParameters = [[MSALSilentTokenParameters alloc] initWithScopes:self.graphScopes
                                                                                            account:account];

    // Attempt to get token silently
    [self.publicClient
     acquireTokenSilentWithParameters:silentParameters
     completionBlock:^(MSALResult * _Nullable result, NSError * _Nullable error) {
         // Check error
         if (error) {
             completionBlock(nil, error);
             return;
         }

         // Check result
         if (!result) {
             NSMutableDictionary* details = [NSMutableDictionary dictionary];
             [details setValue:@"No result was returned" forKey:NSDebugDescriptionErrorKey];
             completionBlock(nil, [NSError errorWithDomain:@"AuthenticationManager" code:0 userInfo:details]);
             return;
         }

         NSLog(@"Got token silently: %@", result.accessToken);
         completionBlock(result.accessToken, nil);
     }];
}

- (void) signOut {
    NSError* msalError;
    NSArray* accounts = [self.publicClient allAccounts:&msalError];

    if (msalError) {
        NSLog(@"Error getting accounts from cache: %@", msalError.debugDescription);
        return;
    }

    for (id account in accounts) {
        [self.publicClient removeAccount:account error:nil];
    }
}

@end
// </AuthManagerSnippet>
